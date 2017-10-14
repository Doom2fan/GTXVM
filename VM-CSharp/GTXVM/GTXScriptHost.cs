using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GTXVM {
    public class GTXScriptHost {
        private Dictionary<uint, SpecialDelegate> specials;
        private List<GTXLibrary> libs;

        public Dictionary<uint, SpecialDelegate> Specials {
            get { return specials; }
            set { specials = value; }
        }
        public List<GTXLibrary> Libs {
            get { return libs; }
            set { libs = value; }
        }

        public GTXScriptHost (List<GTXLibrary> newLibs, Dictionary<uint, SpecialDelegate> spcs) {
            libs = newLibs;
            specials = spcs;
        }

        public void Run (bool singleOp) {
            foreach (GTXLibrary lib in libs)
                lib.Run (singleOp);
        }
        public void Reset () {
            foreach (GTXLibrary lib in libs)
                lib.StopAll ();
        }
    }
}
