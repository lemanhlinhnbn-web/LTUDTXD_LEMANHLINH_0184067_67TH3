using System;
using System.Collections.Generic;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

public interface IEtabsService : IDisposable
{
    bool IsConnected { get; }
    EtabsConnectionResult ConnectToRunningInstance();
    IReadOnlyList<EtabsFrameItem> GetFrameList();
    EtabsForceReadResult ReadFrameForces(string frameName);
    EtabsForceReadResult ReadSelectedFrameForces();
    void Disconnect();
}
