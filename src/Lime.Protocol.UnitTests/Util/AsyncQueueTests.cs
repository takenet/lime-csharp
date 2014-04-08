using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lime.Protocol.Util;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lime.Protocol.UnitTests.Util
{
    [TestClass]
    public class AsyncQueueTests
    {
        private AsyncQueue<T> GetTarget<T>(int promisesLimit = 0, int bufferLimit = 0)
        {
            return new AsyncQueue<T>(
                promisesLimit,
                bufferLimit);
        }
        #region Enqueue

        [TestMethod]
        [TestCategory("Enqueue")]
        public async Task Enqueue_NoPromisses_GoesToBuffer()
        {
            var target = GetTarget<string>();

            var item1 = DataUtil.CreateRandomString(100);
            var cancellationToken = DataUtil.CreateCancellationToken();

            target.Enqueue(item1);

            var actual = await target.DequeueAsync(cancellationToken);

            Assert.AreEqual(item1, actual);
        }

        [TestMethod]
        [TestCategory("Enqueue")]
        public async Task Enqueue_HasOnePromise_CompletePromise()
        {
            var target = GetTarget<string>();

            var cancellationToken = DataUtil.CreateCancellationToken();

            var promiseTask = target.DequeueAsync(cancellationToken);

            var item1 = DataUtil.CreateRandomString(100);
            target.Enqueue(item1);

            var actual = await promiseTask;

            Assert.AreEqual(item1, actual);
        }

        [TestMethod]
        [TestCategory("Enqueue")]
        public void Enqueue_HasOneCancelledPromise_GoesToBuffer()
        {
            var target = GetTarget<string>();

            var cancellationTokenSource = DataUtil.CreateCancellationTokenSource();

            var promiseTask = target.DequeueAsync(cancellationTokenSource.Token);

            cancellationTokenSource.Cancel();

            Assert.IsTrue(promiseTask.IsCanceled);
            Assert.IsFalse(target.HasPromises);

            var item1 = DataUtil.CreateRandomString(100);
            target.Enqueue(item1);

            Assert.AreEqual(1, target.BufferCount);
        }

        [TestMethod]
        [TestCategory("Enqueue")]
        public async Task Enqueue_HasMultiplePromises_CompletePromises()
        {
            var target = GetTarget<string>();

            var cancellationToken = DataUtil.CreateCancellationToken();

            int promisesCount = DataUtil.CreateRandomInt(50);
            var promiseTaskArray = new Task<string>[promisesCount];

            for (int i = 0; i < promisesCount; i++)
            {
                promiseTaskArray[i] = target.DequeueAsync(cancellationToken);
            }

            for (int i = 0; i < promisesCount; i++)
            {
                Assert.IsFalse(promiseTaskArray[i].IsCompleted);

                var item1 = DataUtil.CreateRandomString(100);
                target.Enqueue(item1);

                Assert.IsTrue(promiseTaskArray[i].IsCompleted);

                var actual = await promiseTaskArray[i];

                Assert.AreEqual(item1, actual);
            }
        }

        [TestMethod]
        [TestCategory("Enqueue")]
        public async Task Enqueue_TwiceHasOnePromise_CompletePromiseAndGoesToBuffer()
        {
            var target = GetTarget<string>();

            var cancellationToken = DataUtil.CreateCancellationToken();

            var promiseTask1 = target.DequeueAsync(cancellationToken);

            var item1 = DataUtil.CreateRandomString(100);
            var item2 = DataUtil.CreateRandomString(100);

            Assert.IsFalse(promiseTask1.IsCompleted);

            target.Enqueue(item1);
            target.Enqueue(item2);

            var promiseTask2 = target.DequeueAsync(cancellationToken);
            Assert.IsTrue(promiseTask2.IsCompleted);

            var actual1 = await promiseTask1;
            var actual2 = await promiseTask2;

            Assert.AreEqual(item1, actual1);
            Assert.AreEqual(item2, actual2);
        }

        [TestMethod]
        [TestCategory("Enqueue")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Enqueue_BufferLimitReached_ThrowsInvalidOperationException()
        {
            var target = GetTarget<string>(
                bufferLimit: 2
                );

            var item1 = DataUtil.CreateRandomString(100);
            var item2 = DataUtil.CreateRandomString(100);
            var item3 = DataUtil.CreateRandomString(100);

            target.Enqueue(item1);
            target.Enqueue(item2);
            target.Enqueue(item3);            
        }

        #endregion

        #region DequeueAsync

        [TestMethod]
        [TestCategory("DequeueAsync")]
        public async Task DequeueAsync_HasBuffer_GetsFromTheBuffer()
        {
            var target = GetTarget<string>();
            var cancellationToken = DataUtil.CreateCancellationToken();

            var item1 = DataUtil.CreateRandomString(100);
            target.Enqueue(item1);

            Assert.IsTrue(target.BufferCount == 1);

            var promiseTask1 = target.DequeueAsync(cancellationToken);
            Assert.IsTrue(promiseTask1.IsCompleted);
            Assert.IsTrue(target.BufferCount == 0);

            var actual1 = await promiseTask1;

            Assert.AreEqual(item1, actual1);
        }

        [TestMethod]
        [TestCategory("DequeueAsync")]
        public async Task DequeueAsync_EmptyBuffer_GetsPromiseAndRaisesPromiseAdded()
        {
            var target = GetTarget<string>();
            bool promiseAddedRaised = false;

            target.PromiseAdded += (sender, e) => promiseAddedRaised = true;

            var cancellationToken = DataUtil.CreateCancellationToken();
            Assert.IsTrue(target.BufferCount == 0);

            var promiseTask1 = target.DequeueAsync(cancellationToken);
            Assert.IsFalse(promiseTask1.IsCompleted);
            Assert.IsTrue(promiseAddedRaised);
        }

        [TestMethod]
        [TestCategory("Dequeue")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Dequeue_PromisesLimitReached_ThrowsInvalidOperationException()
        {
            var target = GetTarget<string>(
                promisesLimit: 2);
            var cancellationToken = DataUtil.CreateCancellationToken();

            var promiseTask1 = target.DequeueAsync(cancellationToken);
            var promiseTask2 = target.DequeueAsync(cancellationToken);
            var promiseTask3 = target.DequeueAsync(cancellationToken);
        }

        #endregion
    }
}

