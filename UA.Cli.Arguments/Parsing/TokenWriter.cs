/*
 * Copyright 2012 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace UA.Cli.Arguments.Parsing
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable",
         Justification = "TokenWriter.Enumerable disposes the disposable members of the TokenWriter class.")]
    public class TokenWriter
    {
        protected readonly ConcurrentQueue<Token> Queue;
        protected readonly BlockingCollection<Token> BlockingCollection;
        protected readonly CancellationTokenSource CancellationTokenSource;
        protected bool IsDisposed;
        protected readonly object DisposeLock;
        protected Exception Exception;

        public TokenWriter()
        {
            this.Queue = new ConcurrentQueue<Token>();
            this.CancellationTokenSource = new CancellationTokenSource();
            this.BlockingCollection = new BlockingCollection<Token>(this.Queue);
            this.IsDisposed = false;
            this.DisposeLock = new object();
        }

        public void Add(Token token)
        {
            this.BlockingCollection.Add(token);
        }

        public IEnumerable<Token> GetConsumingEnumerable()
        {
            return new Enumerable(this);
        }

        // FIX this got called _after_ Dispose
        public void Close()
        {
            lock (this.DisposeLock)
            {
                if (!this.IsDisposed)
                    this.BlockingCollection.CompleteAdding();
            }
        }

        public void Abort(Exception exception)
        {
            this.Exception = exception;
            this.CancellationTokenSource.Cancel(true);
        }

        protected class Enumerable : IEnumerable<Token>, IEnumerator<Token>
        {
            private readonly TokenWriter _writer;
            private IEnumerator<Token> _enumerator;

            public Enumerable(TokenWriter writer)
            {
                this._writer = writer;
            }

            public IEnumerator<Token> GetEnumerator()
            {
                this._enumerator = this._writer.BlockingCollection.GetConsumingEnumerable(this._writer.CancellationTokenSource.Token).GetEnumerator();
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public void Dispose()
            {
                lock (this._writer.DisposeLock)
                {
                    this._writer.IsDisposed = true;
                    this._enumerator.Dispose();
                    this._writer.BlockingCollection.Dispose();
                    this._writer.CancellationTokenSource.Dispose();
                }
            }

            public bool MoveNext()
            {
                try
                {
                    return this._enumerator.MoveNext();
                }
                catch (OperationCanceledException)
                {
                    if (this._writer.Exception != null)
                        throw new AggregateException(this._writer.Exception);

                    throw;
                }
            }

            public void Reset()
            {
                this._enumerator.Reset();
            }

            public Token Current => this._enumerator.Current;

            object IEnumerator.Current => this.Current;
        }
    }
}

