#region Microsoft Public License (Ms-PL)

// // Microsoft Public License (Ms-PL)
// // 
// // This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
// // 
// // 1. Definitions
// // 
// // The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
// // 
// // A "contribution" is the original software, or any additions or changes to the software.
// // 
// // A "contributor" is any person that distributes its contribution under this license.
// // 
// // "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// // 
// // 2. Grant of Rights
// // 
// // (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// // 
// // (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// // 
// // 3. Conditions and Limitations
// // 
// // (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// // 
// // (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// // 
// // (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// // 
// // (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// // 
// // (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

#endregion

using System;
using System.Threading;

namespace Crystalbyte.Equinox.Threading
{
    /// <summary>
    ///   Helper class for invoking tasks with timeout. Overhead is 0,005 ms.
    /// </summary>
    /// <typeparam name = "TResult">The type of the result.</typeparam>
    public sealed class WaitFor<TResult>
    {
        private readonly TimeSpan _timeout;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "WaitFor{T}" /> class, 
        ///   using the specified timeout for all operations.
        /// </summary>
        /// <param name = "timeout">The timeotu.</param>
        public WaitFor(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        /// <summary>
        ///   Executes the specified function within the current thread, aborting it
        ///   if it does not complete within the specified timeout interval.
        /// </summary>
        /// <param name = "function">The function.</param>
        /// <returns>result of the function</returns>
        /// <remarks>
        ///   The performance trick is that we do not interrupt the current
        ///   running thread. Instead, we just create a watcher that will sleep
        ///   until the originating thread terminates or until the timeout is
        ///   elapsed.
        /// </remarks>
        /// <exception cref = "ArgumentNullException">if function is null</exception>
        /// <exception cref = "TimeoutException">if the function does not finish in time </exception>
        public TResult Run(Func<TResult> function)
        {
            if (function == null) {
                throw new ArgumentNullException("function");
            }

            var sync = new object();
            var isCompleted = false;

            WaitCallback watcher = obj =>
                                       {
                                           var watchedThread = obj as Thread;

                                           lock (sync) {
                                               if (!isCompleted) {
                                                   Monitor.Wait(sync, _timeout);
                                               }

                                               if (!isCompleted) {
                                                   watchedThread.Abort();
                                               }
                                           }
                                       };

            try {
                ThreadPool.QueueUserWorkItem(watcher, Thread.CurrentThread);
                return function();
            }
            catch (ThreadAbortException) {
                // This is our own exception.
                Thread.ResetAbort();

                throw new TimeoutException(string.Format("The operation has timed out after {0}.", _timeout));
            }
            finally {
                lock (sync) {
                    isCompleted = true;
                    Monitor.Pulse(sync);
                }
            }
        }

        /// <summary>
        ///   Executes the specified function within the current thread, aborting it
        ///   if it does not complete within the specified timeout interval.
        /// </summary>
        /// <param name = "timeout">The timeout.</param>
        /// <param name = "function">The function.</param>
        /// <returns>result of the function</returns>
        /// <remarks>
        ///   The performance trick is that we do not interrupt the current
        ///   running thread. Instead, we just create a watcher that will sleep
        ///   until the originating thread terminates or until the timeout is
        ///   elapsed.
        /// </remarks>
        /// <exception cref = "ArgumentNullException">if function is null</exception>
        /// <exception cref = "TimeoutException">if the function does not finish in time </exception>
        public static TResult Run(TimeSpan timeout, Func<TResult> function)
        {
            return new WaitFor<TResult>(timeout).Run(function);
        }
    }
}