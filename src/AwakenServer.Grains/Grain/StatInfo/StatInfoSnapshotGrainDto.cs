using AutoMapper;
using AwakenServer.StatInfo;

namespace AwakenServer.Grains.Grain.StatInfo;

[AutoMap(typeof(StatInfoSnapshot))]
public class StatInfoSnapshotGrainDto : StatInfoSnapshot
{
    
}