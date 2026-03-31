using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class StateMachine
{
    readonly List<State> states;
    public State currentState { get; private set; }

    public event Action<State,State> OnStateChanged;
    public event Action<List<State>> OnStatesListChanged;

    public bool HasState(State stateType) => states.Contains(stateType);
    public bool HasState<T>() => HasState(typeof(T));
    public bool HasState(Type stateType) => states.Any(s => s.GetType() == stateType);
    public void SetState(Type stateType)
    {
        var availableStates = states.Where(s => stateType.IsAssignableFrom(s.GetType()));
        if(availableStates.Any() ==false)
        {
            Debug.LogWarning("Type of State:" + stateType + " Not Found in StateMachine");
            return;
        }

        var newState = availableStates.GetRandom();
        SetState(newState);
    }

    public void SetState(int index)
    {
        SetState(states[index]);
    }

    public void SetState<T>()
    {
        SetState(typeof(T));
    }

    public void SetState(State state, bool addIfNotExists = false)
    {
        if (states.Contains(state) == false)
        {
            if (addIfNotExists)
            {
                states.Add(state);
                OnStatesListChanged?.Invoke(states);
                state.stateMachine = this;
            }
            else
            {
                Debug.LogWarning("State:" + state.GetType().Name + " Not Found in StateMachine");
                return;
            }
        }

        currentState?.Exit();
        currentState?.InvokeOnExit();
        var oldState = currentState;
        currentState = state;
        currentState?.Enter();
        currentState?.InvokeOnEnter();
        OnStateChanged?.Invoke(oldState, state);
    }

    public void DoState(float deltaTime) => currentState?.Do(deltaTime);
    public void DoFixedUpdate() => currentState?.DoFixedUpdate();
    public int GetCurrentStateIndex() => states.IndexOf(currentState);
    public void Dispose()
    {
        currentState.Exit();
    }
    StateMachine()
    {
        states = new();
    }

    public class Builder
    {
        readonly StateMachine stateMachine;
        public Builder()
        {
            stateMachine = new();
        }

        public Builder AddState(IEnumerable<State> states)
        {
            foreach (var state in states)
                AddState(state);
            return this;
        }

        public Builder AddState(State state)
        {
            stateMachine.states.Add(state);
            state.stateMachine = stateMachine; // Set the stateMachine reference in the state
            return this;
        }

        /// <summary>
        /// Adds a state if it doesn't already exist and sets it as the default state. Also calls Enter on the state.
        /// </summary>
        /// <param name="state">Default State</param>
        /// <returns></returns>
        public Builder AddDefaultState(State state)
        {
            if(stateMachine.states.Contains(state) == false)
            {
                AddState(state);
            }

            stateMachine.currentState = state;
            return this;
        }
        public Builder SetDefaultState(Type stateType)
        {
            var state = stateMachine.states.FirstOrDefault(s => s.GetType() == stateType);
            if (state != null)
            {
                AddDefaultState(state);
            }
            return this;
        }

        public StateMachine Build()
        {
            if (stateMachine.currentState != null)
                stateMachine.currentState.Enter();
            return stateMachine;
        }
    }
}

public abstract class State
{
    public delegate void OnEnterCallback();
    public delegate void OnExitCallback();
    public delegate void OnCompletedCallback();
    event OnEnterCallback onEnter;
    event OnExitCallback onExit;
    event OnCompletedCallback onCompleted;

    public StateMachine stateMachine { get; internal set; }

    public State OnEnter(OnEnterCallback callback)
    {
        onEnter += callback;
        return this;
    }
    public State OnExit(OnExitCallback callback)
    {
        onExit += callback;
        return this;
    }
    public State OnComplete(OnCompletedCallback callback)
    {
        onCompleted += callback;
        return this;
    }

    public void Complete() => onCompleted?.Invoke();
    internal void InvokeOnEnter() => onEnter?.Invoke();
    internal void InvokeOnExit() => onExit?.Invoke();

    public abstract void Enter();
    public abstract void Do(float deltaTime);
    public abstract void DoFixedUpdate();
    public abstract void Exit();
}
