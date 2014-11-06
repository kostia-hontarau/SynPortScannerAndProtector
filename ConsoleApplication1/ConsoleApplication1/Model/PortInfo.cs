using System;

namespace ConsoleApplication1.Model
{
    internal struct PortInfo : IComparable<PortInfo>
    {
        #region Data Members
        private readonly int number;
        private readonly bool isOpen; 
        #endregion

        #region Properties
        public int Number
        {
            get { return this.number; }
        }
        public bool IsOpen
        {
            get { return this.isOpen; }
        } 
        #endregion

        #region Constructors
        public PortInfo(int number, bool isOpen)
        {
            if ((0 < number) && (number < 65535)) this.number = number;
            else throw new ArgumentException("The port number should be between 0 and 65535!", "number");
            this.isOpen = isOpen;
        } 
        #endregion

        #region IComparable<PortInfo>
        public int CompareTo(PortInfo other)
        {
            if (this.number == other.number) return 0;
            if (this.number > other.number) return 1;
            return -1;
        } 
        #endregion
    }
}
