using System;


namespace ConsoleApplication1.Model
{
    internal struct PortInfo : IComparable<PortInfo>
    {
        #region Data Members
        private readonly ushort number;
        private readonly bool isOpen; 
        #endregion

        #region Properties
        public ushort Number
        {
            get { return this.number; }
        }
        public bool IsOpen
        {
            get { return this.isOpen; }
        } 
        #endregion

        #region Constructors
        public PortInfo(ushort number, bool isOpen)
        {
            this.number = number;
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

        #region Overriden Members
        public override string ToString()
        {
            return String.Format("Port {0} is {1};", this.Number, this.IsOpen ? "open" : "closed");
        }
        #endregion
    }
}
