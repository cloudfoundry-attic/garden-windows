﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface IOptions
    {
         string MachineIp { get; }
         string ContainerDirectory { get; }
         int ActiveProcessLimit { get; }
    }
}
