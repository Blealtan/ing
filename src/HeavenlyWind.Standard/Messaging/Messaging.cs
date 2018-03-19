﻿using System;
using System.Runtime.ExceptionServices;

namespace Sakuno.KanColle.Amatsukaze.Messaging
{
    public interface IProducer<out T>
    {
        event Action<T> Received;
    }

    internal abstract class Chainer<TInput, TOutput>
        : IProducer<TOutput>
    {
        private IProducer<TInput> upstream;

        public Chainer(IProducer<TInput> upstream)
            => this.upstream = upstream
            ?? throw new ArgumentNullException(nameof(upstream));

        private Action<TOutput> downstreams;
        public event Action<TOutput> Received
        {
            add
            {
                if ((downstreams += value) != null)
                    upstream.Received += Send;
            }
            remove
            {
                if ((downstreams -= value) == null)
                    upstream.Received -= Send;
            }
        }

        public abstract void Send(TInput arg);

        protected void SendToDownstream(TOutput value)
        {
            var temp = downstreams;
            if (temp == null) return;
            var list = temp.GetInvocationList();
            if (list.Length == 1)
                temp(value);
            else
            {
                Exception exception = null;
                foreach (Action<TOutput> invo in list)
                {
                    try
                    {
                        invo(value);
                    }
                    catch (Exception ex)
                    {
                        if (exception == null)
                            exception = ex;
                        else
                            exception = new AggregateException(exception, ex);
                    }
                }
                if (exception is AggregateException agg)
                    throw agg.Flatten();
                else if (exception != null)
                {
                    var di = ExceptionDispatchInfo.Capture(exception);
                    di.Throw();
                }
            }
        }
    }

    internal class Transformer<TInput, TOutput>
        : Chainer<TInput, TOutput>
    {
        private Func<TInput, TOutput> converter;

        public Transformer(IProducer<TInput> upstream, Func<TInput, TOutput> converter)
            : base(upstream)
            => this.converter = converter
            ?? throw new ArgumentNullException(nameof(converter));

        public override void Send(TInput arg) => SendToDownstream(converter(arg));
    }

    internal class Conditioner<T>
        : Chainer<T, T>
    {
        private Predicate<T> predicate;

        public Conditioner(IProducer<T> upstream, Predicate<T> predicate)
            : base(upstream)
            => this.predicate = predicate
            ?? throw new ArgumentNullException(nameof(predicate));

        public override void Send(T arg)
        {
            if (predicate(arg))
                SendToDownstream(arg);
        }
    }
}
