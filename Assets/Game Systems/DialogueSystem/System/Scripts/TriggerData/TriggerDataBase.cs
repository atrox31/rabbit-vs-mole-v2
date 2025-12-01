using System;

namespace DialogueSystem.TriggerData
{
    /// <summary>
    /// Base class for trigger data providers.
    /// </summary>
    /// <typeparam name="TData">The type of data for this trigger data provider</typeparam>
    public abstract class TriggerDataBase<TData> : ITriggerData where TData : class
    {
        public object Data { get; set; }

        public TData TypedData
        {
            get => Data as TData;
            set => Data = value;
        }

        /// <summary>
        /// Gets the output value for a specific port.
        /// </summary>
        public abstract object GetOutputValue(string portName, DialogueSequence sequence = null);

        /// <summary>
        /// Gets the names of all output ports.
        /// </summary>
        public abstract string[] GetOutputPortNames();

        /// <summary>
        /// Gets the type of a specific output port.
        /// </summary>
        public abstract Type GetOutputPortType(string portName);
    }

    /// <summary>
    /// Interface for trigger data providers.
    /// </summary>
    public interface ITriggerData
    {
        object Data { get; set; }
        object GetOutputValue(string portName, DialogueSequence sequence = null);
        string[] GetOutputPortNames();
        Type GetOutputPortType(string portName);
    }
}

