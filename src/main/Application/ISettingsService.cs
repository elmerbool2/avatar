﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ei8.Avatar.Application
{
    public interface ISettingsService
    {
        string CortexInBaseUrl { get; }
        string CortexOutBaseUrl { get; }
        string CortexGraphOutBaseUrl { get; }
        string EventSourcingOutBaseUrl { get; } 
    }
}
