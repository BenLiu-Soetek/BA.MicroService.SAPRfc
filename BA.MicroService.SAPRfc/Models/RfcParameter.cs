namespace BA.MicroService.SAPRfc.Models
{
    public class RfcParameter
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public object Value { get; set; }
        
        public RfcParameter(string name,object value)
        {
            Name = name;
            Value = value;
        }
    }
}
