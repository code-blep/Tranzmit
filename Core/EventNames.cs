namespace Blep.Tranzmit
{
    public partial class Tranzmit
    {
        /// <summary>
        /// To create a new Event Name, add it to the enum below.
        /// Use the intergers to preserve other fields when you modify these enums! 0 to 10 are reserved for Tranzmit.
        /// Also if you accidentaly delete the wrong enum, adding it back in with the same int (Tranzmit Event will show the value!) will restore it back to normal in Trazmit Event.
        /// Also allows for renaming!
        /// Check your Intergers! Duplicates will cause issues!
        /// 
        /// /// 
        ///        CONSIDERING CHECKS LIKE THIS:
        ///        num Status
        ///        {
        ///            OK = 0,
        ///            Warning = 64,
        ///            Error = 256
        ///        }
        ///
        ///        static void Main(string[] args)
        ///        {
        ///            bool exists;
        ///
        ///            // Testing for Integer Values
        ///            exists = Enum.IsDefined(typeof(Status), 0);          // exists = true
        ///            exists = Enum.IsDefined(typeof(Status), 1);          // exists = false
        ///
        ///            // Testing for Constant Names
        ///            exists = Enum.IsDefined(typeof(Status), "OK");       // exists = true
        ///            exists = Enum.IsDefined(typeof(Status), "NotOK");    // exists = false
        ///        }
        /// 
        /// 
        /// </summary>
        public enum EventNames
        {
            PlayerStats = 0,
            Damage = 1,
            SecretFound = 2
        }
    }
}