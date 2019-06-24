using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using TechBrain;

namespace WebApp.Api.DeviceServer
{
    [Route("api/devserver")]
    public class DeviceServerController : ControllerBase
    {
        DevServerConfig DevServerConfig { get; }
        public DeviceServerController(DevServerConfig devServerConfig)
        {
            DevServerConfig = devServerConfig;
        }

        [HttpGet("config")]
        public DevServerConfig GetConfig() => DevServerConfig;
    }
}
