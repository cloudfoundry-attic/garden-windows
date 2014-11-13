using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface IStreamOutService
    {
        System.IO.Stream StreamFile(string id, string source);
    }
}
