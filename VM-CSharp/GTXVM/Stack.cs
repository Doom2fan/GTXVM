using System;
using System.Linq;

/// <summary>
/// Support functions, types, classes and structs for the GTX VM
/// </summary>
namespace GTXVM.Support {
    public class StackException : Exception {
        public StackException () : base () { }
        public StackException (string message) : base (message) { }
        public StackException (string message, Exception innerException) : base (message, innerException) { }
    }

    public struct Stack <T> {
        private T [] _stk;
        private int _size;
        private int _counter;

        public Stack (int sz) {
            _stk = new T [sz];
            _size = sz;
            _counter = -1;
        }
        
        /// <summary>
        /// Gets the maximum size of the stack
        /// </summary>
        public int Size { get { return _size; } }
        /// <summary>
        /// Returns the current amount of values in the stack
        /// </summary>
        public int Count { get { return _counter + 1; } }
        /// <summary>
        /// Returns the 
        /// </summary>
        public bool Empty { get { return _counter == -1; } }
        public bool Full { get { return _counter == _size - 1; } }

        /// <summary>
        /// Pops a value from the stack
        /// </summary>
        /// <returns>The popped value</returns>
        public T Pop () {
            if (_counter == -1)
                throw new StackException ("Attempted to pop a value from an empty stack");

            return _stk [_counter--];
        }
        /// <summary>
        /// Pops the specified amount of values from the stack
        /// </summary>
        /// <param name="count">The amount of values to pop</param>
        /// <returns>An array with the popped values</returns>
        public T [] Pop (uint count) {
            if (_counter == -1)
                throw new StackException ("Attempted to pop a value from an empty stack");

            T [] ret = new T [count];
            for (int i = 0; i < count; i++)
                ret [i] = _stk [_counter--];
            return ret;
        }

        public T [] PopReverse (uint count) {
            return (T []) (this.Pop (count).Reverse ());
        }

        /// <summary>
        /// Gets a value from the stack without consuming it
        /// </summary>
        public T Peek () {
            if (_counter == -1)
                throw new StackException ("Attempted to peek a value from an empty stack");

            return _stk [_counter];
        }
        /// <summary>
        /// Gets the specified amount of values from the stack without consuming them
        /// </summary>
        /// <param name="count">The amount of values to get</param>
        /// <returns>An array with the values</returns>
        public T [] Peek (uint count) {
            if (_counter == -1)
                throw new StackException ("Attempted to pop a value from an empty stack");

            T [] ret = new T [count];
            for (int i = 0; i < count; i++)
                ret [i] = _stk [_counter - i];
            return ret;
        }

        /// <summary>
        /// Pushes a value to the stack, and returns the success state
        /// </summary>
        /// <param name="val">The value to be pushed</param>
        /// <returns>Returns true if the value was pushed to the stack; Returns false if the stack is full</returns>
        public bool Push (T val) {
            if (_counter == _size -1)
                return false;

            _stk [++_counter] = val;
            return true;
        }
        /// <summary>
        /// Pushes an array of values to the stack
        /// </summary>
        /// <param name="val">An array of values</param>
        public void Push (T [] val) {
            if (_counter == _size - 1)
                throw new StackException ("The stack is full");

            for (int i = 0; i < val.Length; i++)
                _stk [++_counter] = val [i];
        }

        /// <summary>
        /// Resets the stack counter to 0
        /// </summary>
        public void Clear () { _counter = -1; }
    }
}
