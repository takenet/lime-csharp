using System;
using Xunit;
using Lime.Protocol.Util;
using System.Threading.Tasks;
using Shouldly;

namespace Lime.Protocol.UnitTests.Util
{
    
    public class AsyncQueueTests
    {
        private AsyncQueue<T> GetTarget<T>(int promisesLimit = 0, int bufferLimit = 0)
        {
            return new AsyncQueue<T>(
                promisesLimit,
                bufferLimit);
        }
        #region Post

        [Fact]
        [Trait("Category", "Post")]
        public async Task Post_NoPromisses_GoesToBuffer()
        {
            var target = GetTarget<string>();

            var item1 = Dummy.CreateRandomString(100);
            var cancellationToken = Dummy.CreateCancellationToken();

			Assert.True(target.Post(item1));

            var actual = await target.ReceiveAsync(cancellationToken);

            Assert.Equal(item1, actual);
        }

        [Fact]
        [Trait("Category", "Post")]
        public async Task Post_HasOnePromise_CompletePromise()
        {
            var target = GetTarget<string>();

            var cancellationToken = Dummy.CreateCancellationToken();

            var promiseTask = target.ReceiveAsync(cancellationToken);

            var item1 = Dummy.CreateRandomString(100);
			Assert.True(target.Post(item1));

            var actual = await promiseTask;

            Assert.Equal(item1, actual);
        }

        [Fact]
        [Trait("Category", "Post")]
        public void Post_HasOneCancelledPromise_GoesToBuffer()
        {
            var target = GetTarget<string>();

            var cancellationTokenSource = Dummy.CreateCancellationTokenSource();

            var promiseTask = target.ReceiveAsync(cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            Assert.True(promiseTask.IsCanceled);
            Assert.False(target.HasPromises);

            var item1 = Dummy.CreateRandomString(100);
			Assert.True(target.Post(item1));

            Assert.Equal(1, target.BufferCount);
        }

        [Fact]
        [Trait("Category", "Post")]
        public async Task Post_HasMultiplePromises_CompletePromises()
        {
            var target = GetTarget<string>();

            var cancellationToken = Dummy.CreateCancellationToken();

            int promisesCount = Dummy.CreateRandomInt(50);
            var promiseTaskArray = new Task<string>[promisesCount];

            for (int i = 0; i < promisesCount; i++)
            {
                promiseTaskArray[i] = target.ReceiveAsync(cancellationToken);
            }

            for (int i = 0; i < promisesCount; i++)
            {
                Assert.False(promiseTaskArray[i].IsCompleted);

                var item1 = Dummy.CreateRandomString(100);
				Assert.True(target.Post(item1));

                Assert.True(promiseTaskArray[i].IsCompleted);

                var actual = await promiseTaskArray[i];

                Assert.Equal(item1, actual);
            }
        }

        [Fact]
        [Trait("Category", "Post")]
        public async Task Post_TwiceHasOnePromise_CompletePromiseAndGoesToBuffer()
        {
            var target = GetTarget<string>();

            var cancellationToken = Dummy.CreateCancellationToken();

            var promiseTask1 = target.ReceiveAsync(cancellationToken);

            var item1 = Dummy.CreateRandomString(100);
            var item2 = Dummy.CreateRandomString(100);

            Assert.False(promiseTask1.IsCompleted);

			Assert.True(target.Post(item1));
			Assert.True(target.Post(item2));

            var promiseTask2 = target.ReceiveAsync(cancellationToken);
            Assert.True(promiseTask2.IsCompleted);

            var actual1 = await promiseTask1;
            var actual2 = await promiseTask2;

            Assert.Equal(item1, actual1);
            Assert.Equal(item2, actual2);
        }

        [Fact]
        [Trait("Category", "Post")]
		public void Post_BufferLimitReached_ReturnsFalse()
        {
            var target = GetTarget<string>(
                bufferLimit: 2
                );

            var item1 = Dummy.CreateRandomString(100);
            var item2 = Dummy.CreateRandomString(100);
            var item3 = Dummy.CreateRandomString(100);

			Assert.True(target.Post(item1));
			Assert.True(target.Post(item2));
			Assert.False(target.Post(item3));
        }

        #endregion

        #region ReceiveAsync

        [Fact]
        [Trait("Category", "ReceiveAsync")]
        public async Task ReceiveAsync_HasBuffer_GetsFromTheBuffer()
        {
            var target = GetTarget<string>();
            var cancellationToken = Dummy.CreateCancellationToken();

            var item1 = Dummy.CreateRandomString(100);
            target.Post(item1);

            Assert.True(target.BufferCount == 1);

            var promiseTask1 = target.ReceiveAsync(cancellationToken);
            Assert.True(promiseTask1.IsCompleted);
            Assert.True(target.BufferCount == 0);

            var actual1 = await promiseTask1;

            Assert.Equal(item1, actual1);
        }

        [Fact]
        [Trait("Category", "Dequeue")]
        public void Dequeue_PromisesLimitReached_ThrowsInvalidOperationException()
        {
            var target = GetTarget<string>(
                promisesLimit: 2);
            var cancellationToken = Dummy.CreateCancellationToken();

            target.ReceiveAsync(cancellationToken);
            target.ReceiveAsync(cancellationToken);
            Should.Throw<InvalidOperationException>( () => 
                target.ReceiveAsync(cancellationToken));
        }

        [Fact]
        [Trait("Category", "Dequeue")]
        [Trait("Category", "Post")]
        public async Task DequeuePost_ConcurrentAccess_CompletePromises()
        {
            var count = Dummy.CreateRandomInt(1000);
            var cancellationToken = Dummy.CreateCancellationToken();

            var target = GetTarget<string>(count, count);

            var PostTasks = new Task[count];
            var dequeueTasks = new Task<string>[count];           

            var PostSetupTask = Task.Run(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        var item = Dummy.CreateRandomString(100);
                        PostTasks[i] = Task.Run(() => target.Post(item));
                    }
                });

            var dequeueSetupTask = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var item = Dummy.CreateRandomString(100);
                    dequeueTasks[i] = target.ReceiveAsync(cancellationToken);
                }
            });

            await Task.WhenAll(PostSetupTask, dequeueSetupTask);
            await Task.WhenAll(dequeueTasks);
            await Task.WhenAll(PostTasks);

            Assert.True(target.BufferCount == 0);
            Assert.True(target.PromisesCount == 0);
        }

        #endregion
    }
}