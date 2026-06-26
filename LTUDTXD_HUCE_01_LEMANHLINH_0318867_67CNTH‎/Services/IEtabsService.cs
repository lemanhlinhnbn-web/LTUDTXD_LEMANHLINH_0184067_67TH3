using System;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Models;

namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Services;

public interface IEtabsService : IDisposable
{
    bool IsConnected { get; }
    EtabsConnectionResult ConnectToRunningInstance();
    void Disconnect();
}
