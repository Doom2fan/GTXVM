using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GTXVM {
    public delegate void SpecialDelegate (GTXScript script, byte [] argBytes);
    public class GTXLibrary {
        private Dictionary<uint, GTXScriptData> scripts;
        private Dictionary<string, GTXScriptData> namedScripts;
        private Dictionary<uint, string> strTable;
        private Dictionary<uint, GTXScript> runningScripts;
        private GTXScriptHost ownerHost;

        public Dictionary<uint, GTXScriptData> Scripts { get { return scripts; } }
        public Dictionary<string, GTXScriptData> NamedScripts { get { return namedScripts; } }
        public Dictionary<uint, string> StringTable { get { return strTable; } }
        public Dictionary<uint, SpecialDelegate> Specials {
            get { return ownerHost.Specials; }
        }
        public GTXScriptHost ScriptHost {
            get { return ownerHost; }
            set { ownerHost = value; }
        }


        public GTXLibrary (GTXScriptHost host, Dictionary<uint, string> newStrTbl = null, Dictionary<uint, GTXScriptData> libScripts = null, Dictionary<string, GTXScriptData> libNamedScripts = null) {
            ownerHost = host;
            strTable = (newStrTbl == null ? new Dictionary<uint, string> () : newStrTbl);
            scripts = (libScripts == null ? new Dictionary<uint, GTXScriptData> () : libScripts);
            namedScripts = (libNamedScripts == null ? new Dictionary<string, GTXScriptData> () : libNamedScripts);
            runningScripts = new Dictionary<uint, GTXScript> ();
        }

        private GTXLibrary LoadLibFromStream (Stream stream) {
            throw new Exception ();
        }
        public GTXLibrary FromStream (Stream stream) {
            return LoadLibFromStream (stream);
        }
        public GTXLibrary FromFile (string file) {
            using (var stream = new FileStream (file, FileMode.Open, FileAccess.Read))
                return LoadLibFromStream (stream);
        }

        public void Run (bool singleOp = false) {
            foreach (GTXScript sc in runningScripts.Values)
                sc.Run (singleOp);
        }
        
        public uint StartScript (uint id, byte [] args) {
            if (!scripts.ContainsKey (id))
                throw new ArgumentException ("The specified script ID does not exist");
            
            GTXScriptData scData = scripts [id];
            GTXScript script = new GTXScript (this, scData.MemorySize, scData.StackSize);
            Support.Stack<byte> stk = new Support.Stack<byte> (scData.StackSize);
            script.Stack = stk;

            uint pID = (uint) script.GetHashCode ();
            script.PID = pID;
            runningScripts.Add (pID, script);
            return pID;
        }

        public uint StartNamedScript (string name, byte [] args) {
            if (!namedScripts.ContainsKey (name))
                throw new ArgumentException ("The specified script name does not exist");

            GTXScriptData scData = namedScripts [name];
            GTXScript script = new GTXScript (this, scData.MemorySize, scData.StackSize);
            Support.Stack<byte> stk = new Support.Stack<byte> (scData.StackSize);
            script.Stack = stk;

            uint pID = (uint) script.GetHashCode ();
            script.PID = pID;
            runningScripts.Add (pID, script);
            return pID;
        }

        public GTXScript GetRunningScript (uint pID) {
            if (runningScripts.ContainsKey (pID))
                return runningScripts [pID];
            else
                return null;
        }

        public void StopAll () {
            foreach (GTXScript sc in runningScripts.Values)
                sc.State = GTXScriptState.Terminated;
            runningScripts.Clear ();
        }

        /// <summary>
        /// Adds a string to the library's static string table
        /// </summary>
        /// <param name="id">The string's id</param>
        /// <param name="str">The string to add</param>
        /// <returns>Returns true if the string was successfully added, false otherwise. This method returns false if there's already a string with the specified name in the table</returns>
        public bool AddString (uint id, string str) {
            if (scripts.ContainsKey (id))
                return false;
            else {
                strTable.Add (id, str);
                return true;
            }
        }

        /// <summary>
        /// Adds a script to the library
        /// </summary>
        /// <param name="id">The script's id</param>
        /// <param name="str">The script to add</param>
        /// <returns>Returns true if the script was successfully added, false otherwise. This method returns false if there's already a script with the specified id in the library</returns>
        public bool AddScript (uint id, ref GTXScriptData script) {
            if (scripts.ContainsKey (id))
                return false;
            else {
                scripts.Add (id, script);
                return true;
            }
        }

        /// <summary>
        /// Adds a named script to the library
        /// </summary>
        /// <param name="name">The script's name</param>
        /// <param name="str">The script to add</param>
        /// <returns>Returns true if the script was successfully added, false otherwise. This method returns false if there's already a script with the specified name in the library</returns>
        public bool AddNamedScript (string name, ref GTXScriptData script) {
            if (namedScripts.ContainsKey (name))
                return false;
            else {
                namedScripts.Add (name, script);
                return true;
            }
        }
        /// <summary>
        /// Removes a script from the library
        /// </summary>
        /// <param name="id">The script's ID</param>
        /// <returns>Returns true if the script was successfully removed, false otherwise. This method returns false if the script does not exist</returns>
        public bool RemoveScript (uint id) {
            if (!scripts.ContainsKey (id))
                return false;
            else {
                return scripts.Remove (id);
            }
        }

        /// <summary>
        /// Removes a named script from the library
        /// </summary>
        /// <param name="name">The script's name</param>
        /// <returns>Returns true if the script was successfully removed, false otherwise. This method returns false if the script does not exist</returns>
        public bool RemoveNamedScript (string name) {
            if (!namedScripts.ContainsKey (name))
                return false;
            else {
                return namedScripts.Remove (name);
            }
        }
    }
}
