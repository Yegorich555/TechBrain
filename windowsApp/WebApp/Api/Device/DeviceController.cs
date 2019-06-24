using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TechBrain;
using TechBrain.Services;

namespace WebApp.Api.Device
{
    [Route("api/devices")]
    public class DeviceController : ControllerBase
    {
        DevServer DevServer { get; }

        public DeviceController(DevServer devServer)
        {
            DevServer = devServer;
        }

        [HttpGet]
        public IEnumerable<TechBrain.Entities.Device> GetAll() => DevServer.DeviceRepository.GetAll();
    }
}
