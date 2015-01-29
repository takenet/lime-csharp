using System;
using System.Linq;
using NUnit.Framework;
using Lime.Protocol.Util;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lime.Protocol.UnitTests.Util
{
    [TestFixture]
    public class AsyncQueueTests
    {
        private AsyncQueue<T> GetTarget<T>(int promisesLimit = 0, int bufferLimit = 0)
        {
            return new AsyncQueue<T>(
                promisesLimit,
                bufferLimit);
        }
        #region Post

        [Test]
        [Category("Post")]
        public async Task Post_NoPromisses_GoesToBuffer()
        {
            var target = GetTarget<string>();

            var item1 = DataUtil.CreateRandomString(100);
            var cancellationToken = DataUtil.CreateCancellationToken();

			Assert.IsTrue(target.Post(item1));

            var actual = await target.ReceiveAsync(cancellationToken);

            Assert.AreEqual(item1, actual);
        }

        [Test]
        [Category("Post")]
        public async Task Post_HasOnePromise_CompletePromise()
        {
            var target = GetTarget<string>();

            var cancellationToken = DataUtil.CreateCancellationToken();

            var promiseTask = target.ReceiveAsync(cancellationToken);

            var item1 = DataUtil.CreateRandomString(100);
			Assert.IsTrue(target.Post(item1));

            var actual = await promiseTask;

            Assert.AreEqual(item1, actual);
        }

        [Test]
        [Category("Post")]
        public void Post_HasOneCancelledPromise_GoesToBuffer()
        {
            var target = GetTarget<string>();

            var cancellationTokenSource = DataUtil.CreateCancellationTokenSource();

            var promiseTask = target.ReceiveAsync(cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            Assert.IsTrue(promiseTask.IsCanceled);
            Assert.IsFalse(target.HasPromises);

            var item1 = DataUtil.CreateRandomString(100);
			Assert.IsTrue(target.Post(item1));

            Assert.AreEqual(1, target.BufferCount);
        }

        [Test]
        [Category("Post")]
        public async Task Post_HasMultiplePromises_CompletePromises()
        {
            var target = GetTarget<string>();

            var cancellationToken = DataUtil.CreateCancellationToken();

            int promisesCount = DataUtil.CreateRandomInt(50);
            var promiseTaskArray = new Task<string>[promisesCount];

            for (int i = 0; i < promisesCount; i++)
            {
                promiseTaskArray[i] = target.ReceiveAsync(cancellationToken);
            }

            for (int i = 0; i < promisesCount; i++)
            {
                Assert.IsFalse(promiseTaskArray[i].IsCompleted);

                var item1 = DataUtil.CreateRandomString(100);
				Assert.IsTrue(target.Post(item1));

                Assert.IsTrue(promiseTaskArray[i].IsCompleted);

                var actual = await promiseTaskArray[i];

                Assert.AreEqual(item1, actual);
            }
        }

        [Test]
        [Category("Post")]
        public async Task Post_TwiceHasOnePromise_CompletePromiseAndGoesToBuffer()
        {
            var target = GetTarget<string>();

            var cancellationToken = DataUtil.CreateCancellationToken();

            var promiseTask1 = target.ReceiveAsync(cancellationToken);

            var item1 = DataUtil.CreateRandomString(100);
            var item2 = DataUtil.CreateRandomString(100);

            Assert.IsFalse(promiseTask1.IsCompleted);

			Assert.IsTrue(target.Post(item1));
			Assert.IsTrue(target.Post(item2));

            var promiseTask2 = target.ReceiveAsync(cancellationToken);
            Assert.IsTrue(promiseTask2.IsCompleted);

            var actual1 = await promiseTask1;
            var actual2 = await promiseTask2;

            Assert.AreEqual(item1, actual1);
            Assert.AreEqual(item2, actual2);
        }

        [Test]
        [Category("Post")]
		public void Post_BufferLimitReached_ReturnsFalse()
        {
            var target = GetTarget<string>(
                bufferLimit: 2
                );

            var item1 = DataUtil.CreateRandomString(100);
            var item2 = DataUtil.CreateRandomString(100);
            var item3 = DataUtil.CreateRandomString(100);

			Assert.IsTrue(target.Post(item1));
			Assert.IsTrue(target.Post(item2));
			Assert.IsFalse(target.Post(item3));
        }

        #endregion

        #region ReceiveAsync

        [Test]
        [Category("ReceiveAsync")]
        public async Task ReceiveAsync_HasBuffer_GetsFromTheBuffer()
        {
            var target = GetTarget<string>();
            var cancellationToken = DataUtil.CreateCancellationToken();

            var item1 = DataUtil.CreateRandomString(100);
            target.Post(item1);

            Assert.IsTrue(target.BufferCount == 1);

            var promiseTask1 = target.ReceiveAsync(cancellationToken);
            Assert.IsTrue(promiseTask1.IsCompleted);
            Assert.IsTrue(target.BufferCount == 0);

            var actual1 = await promiseTask1;

            Assert.AreEqual(item1, actual1);
        }

        [Test]
        [Category("Dequeue")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Dequeue_PromisesLimitReached_ThrowsInvalidOperationException()
        {
            var target = GetTarget<string>(
                promisesLimit: 2);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var promiseTask1 = target.ReceiveAsync(cancellationToken);
            var promiseTask2 = target.ReceiveAsync(cancellationToken);
            var promiseTask3 = target.ReceiveAsync(cancellationToken);
        }

        [Test]
        [Category("Dequeue")]
        [Category("Post")]
        public async Task DequeuePost_ConcurrentAccess_CompletePromises()
        {
            var count = DataUtil.CreateRandomInt(1000);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var target = GetTarget<string>(count, count);

            var PostTasks = new Task[count];
            var dequeueTasks = new Task<string>[count];           

            var PostSetupTask = Task.Run(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        var item = DataUtil.CreateRandomString(100);
                        PostTasks[i] = Task.Run(() => target.Post(item));
                    }
                });

            var dequeueSetupTask = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var item = DataUtil.CreateRandomString(100);
                    dequeueTasks[i] = target.ReceiveAsync(cancellationToken);
                }
            });

            await Task.WhenAll(PostSetupTask, dequeueSetupTask);
            await Task.WhenAll(dequeueTasks);
            await Task.WhenAll(PostTasks);

            Assert.IsTrue(target.BufferCount == 0);
            Assert.IsTrue(target.PromisesCount == 0);
        }

        #endregion
    }
}