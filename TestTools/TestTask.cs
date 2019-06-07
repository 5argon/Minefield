using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace System.Runtime.CompilerServices
{
    public sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public Type BuilderType { get; }

        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }
    }
}

namespace E7.Minefield
{
    public static class AwaiterExtension
    {
        public static Awaiter GetAwaiter<T>(this int lazy)
        {
            return new Awaiter();
        }
    }

    public struct Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action continuation)
        {
            throw new NotImplementedException();
        }
    }

    [AsyncMethodBuilder(typeof(TaskLikeMethodBuilder))]
    public class TestTask
    {
        public Awaiter GetAwaiter()
        {
            return new Awaiter();
        }

        public static implicit operator TestTask(float time) => new TestTask();
    }

    public sealed class TaskLikeMethodBuilder
    {
        public TaskLikeMethodBuilder()
        {
        }

        public static TaskLikeMethodBuilder Create()
            => new TaskLikeMethodBuilder();

        public void SetResult() 
        {
            Debug.Log($"SetResult");
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            Debug.Log($"Start");
            stateMachine.MoveNext();
        }

        public TestTask Task => default(TestTask);

        public void SetException(Exception e)
        {
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion 
        where TStateMachine : IAsyncStateMachine
        {
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) 
        where TAwaiter : ICriticalNotifyCompletion 
        where TStateMachine : IAsyncStateMachine
        {
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }
}