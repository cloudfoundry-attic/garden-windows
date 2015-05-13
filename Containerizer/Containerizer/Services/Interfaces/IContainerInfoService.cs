using Containerizer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containerizer.Services.Interfaces
{
    public interface IContainerInfoService
    {
        ContainerInfoApiModel GetInfoByHandle(string handle);
        ContainerMetricsApiModel GetMetricsByHandle(string handle);
    }
}
