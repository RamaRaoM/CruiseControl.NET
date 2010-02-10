﻿namespace ThoughtWorks.CruiseControl.Core.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using Exortech.NetReflector;
    using ThoughtWorks.CruiseControl.Remote;
    using ThoughtWorks.CruiseControl.Remote.Parameters;

    /// <summary>
    /// An abstract base class to add parameters to a task
    /// </summary>
    public abstract class TaskBase
        : IParamatisedItem, IStatusSnapshotGenerator, ITask
    {
        #region Private fields
        private IDynamicValue[] myDynamicValues = new IDynamicValue[0];
        private ItemStatus currentStatus;
        private List<TimeSpan> elapsedTimes = new List<TimeSpan>();
        private string name;
        #endregion

        #region Public properties
        #region DynamicValues
        /// <summary>
        /// The dynamic values to use for the task.
        /// </summary>
        [ReflectorProperty("dynamicValues", Required = false)]
        public IDynamicValue[] DynamicValues
        {
            get { return myDynamicValues; }
            set { myDynamicValues = value;}
        }
        #endregion

        #region Name
        /// <summary>
        /// The name of the task - by default this is the name of the type.
        /// </summary>
        [ReflectorProperty("name", Required = false)]
        public virtual string Name
        {
            get { return string.IsNullOrEmpty(name) ? GetType().Name : name; }
            set { name = value; }
        }
        #endregion

        #region Description
        /// <summary>
        /// Description used for the visualisation of the buildstage, if left empty the process name will be shown
        /// </summary>
        [ReflectorProperty("description", Required = false)]
        public string Description { get; set; }
        #endregion

        #region CurrentStatus
        /// <summary>
        /// The current status of the task.
        /// </summary>
        public ItemStatus CurrentStatus
        {
            get { return currentStatus; }
        }
        #endregion

        #region Context
        /// <summary>
        /// Gets the context for the task..
        /// </summary>
        /// <value>The context.</value>
        public TaskContext Context { get; private set; }
        #endregion
        #endregion

        #region Public methods
        #region Run()
        /// <summary>
        /// Runs the task, given the specified <see cref="IIntegrationResult"/>, in the specified <see cref="IProject"/>.
        /// </summary>
        /// <param name="result"></param>
        public virtual void Run(IIntegrationResult result)
        {
            // Initialise the task
            InitialiseStatus();
            currentStatus.Status = ItemBuildStatus.Running;
            currentStatus.TimeOfEstimatedCompletion = CalculateEstimatedTime();
            currentStatus.TimeStarted = DateTime.Now;

            // Perform the actual run
            var taskSuccess = false;
            try
            {
                taskSuccess = Execute(result);
            }
            catch (Exception error)
            {
                // Store the error message
                currentStatus.Error = error.Message;
                result.Status = IntegrationStatus.Exception;
                throw;
            }
            finally
            {
                // Clean up
                currentStatus.Status = (taskSuccess) ? ItemBuildStatus.CompletedSuccess : ItemBuildStatus.CompletedFailed;
                currentStatus.TimeCompleted = DateTime.Now;
                if (result.Status == IntegrationStatus.Unknown)
                {
                    result.Status = taskSuccess ? IntegrationStatus.Success : IntegrationStatus.Failure;
                }
            }
        }
        #endregion

        #region CalculateEstimatedTime()
        /// <summary>
        /// Calculate the estimated time of completion.
        /// </summary>
        /// <returns></returns>
        public virtual DateTime? CalculateEstimatedTime()
        {
            // Calculate the estimated completion time
            double seconds = 0;
            if (elapsedTimes.Count > 0)
            {
                for (var loop = 0; loop < elapsedTimes.Count; loop++)
                {
                    seconds += elapsedTimes[loop].TotalSeconds;
                }
                seconds /= elapsedTimes.Count;
            }
            return seconds > 0 ? (DateTime?)DateTime.Now.AddSeconds(seconds) : null;
        }
        #endregion

        #region GenerateSnapshot()
        /// <summary>
        /// Generates a snapshot of the current status.
        /// </summary>
        /// <returns></returns>
        public virtual ItemStatus GenerateSnapshot()
        {
            if (currentStatus == null) InitialiseStatus();
            return currentStatus;
        }
        #endregion

        #region ApplyParameters()
        /// <summary>
        /// Applies the input parameters to the task.
        /// </summary>
        /// <param name="parameters">The parameters to apply.</param>
        /// <param name="parameterDefinitions">The original parameter definitions.</param>
        public virtual void ApplyParameters(Dictionary<string, string> parameters, IEnumerable<ParameterBase> parameterDefinitions)
        {
            if (myDynamicValues != null)
            {
                foreach (IDynamicValue value in myDynamicValues)
                {
                    value.ApplyTo(this, parameters, parameterDefinitions);
                }
            }
        }
        #endregion

        #region PreprocessParameters()
        /// <summary>
        /// Preprocesses a node prior to loading it via NetReflector.
        /// </summary>
        /// <param name="inputNode"></param>
        /// <returns></returns>
        [ReflectionPreprocessor]
        public virtual XmlNode PreprocessParameters(XmlNode inputNode)
        {
            return DynamicValueUtility.ConvertXmlToDynamicValues(inputNode);
        }
        #endregion

        #region RetrieveDescriptionOrName()
        /// <summary>
        /// Retrieves the description if it is set, otherwise the name of the task.
        /// </summary>
        /// <returns>The description or name of the task.</returns>
        public virtual string RetrieveDescriptionOrName()
        {
            var value = string.IsNullOrEmpty(Description) ? Name : Description;
            return value;
        }
        #endregion

        #region InitialiseStatus()
        /// <summary>
        /// Initialise an <see cref="ItemStatus"/>.
        /// </summary>
        public virtual void InitialiseStatus()
        {
            // Store the last elapsed time
            if (currentStatus != null)
            {
                var elapsedTime = currentStatus.TimeCompleted - currentStatus.TimeStarted;
                if (elapsedTime.HasValue)
                {
                    if (elapsedTimes.Count >= 8)
                    {
                        elapsedTimes.RemoveAt(7);
                    }
                    elapsedTimes.Insert(0, elapsedTime.Value);
                }
            }

            // Initialise the status with the default value
            currentStatus = new ItemStatus
            {
                Name = Name,
                Description = Description,
                Status = ItemBuildStatus.Pending,
                TimeCompleted = null,
                TimeOfEstimatedCompletion = null,
                TimeStarted = null
            };
        }
        #endregion

        #region AssociateContext()
        /// <summary>
        /// Associates the context for the task.
        /// </summary>
        /// <param name="context">The context.</param>
        public void AssociateContext(TaskContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context", "context is null.");
            }

            this.Context = context;
            this.Context.InitialiseResult(this.RetrieveType(), this.RetrieveDescriptionOrName());
        }
        #endregion
        #endregion

        #region Protected methods
        #region Execute()
        /// <summary>
        /// Execute the actual task functionality.
        /// </summary>
        /// <param name="result"></param>
        /// <returns>True if the task was successful, false otherwise.</returns>
        protected abstract bool Execute(IIntegrationResult result);
        #endregion

        /// <summary>
        /// Retrieves the type of the task.
        /// </summary>
        /// <returns>The task type as defined by the first <see cref="ReflectorTypeAttribute"/>.</returns>
        protected virtual string RetrieveType()
        {
            var typeDefinition = this.GetType().GetCustomAttributes(typeof(ReflectorTypeAttribute), false);
            if (typeDefinition.Length > 0)
            {
                return (typeDefinition[0] as ReflectorTypeAttribute).Name;
            }
            else
            {
                return "{Unknown task type}";
            }
        }
        #endregion
    }
}
