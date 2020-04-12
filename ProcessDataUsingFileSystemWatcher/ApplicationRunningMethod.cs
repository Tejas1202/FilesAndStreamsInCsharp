using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataUsingFileSystemWatcher
{
    public enum ApplicationRunningMethod
    {
        Normally = 1,
        UsingConcurrentDictionary = 2,
        UsingMemoryCache = 3
    }
}
