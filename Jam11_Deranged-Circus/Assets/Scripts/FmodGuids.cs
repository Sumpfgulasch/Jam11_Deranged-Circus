/*
    FmodGuids.cs - FMOD Studio API

    Generated GUIDs for project 'Deranged-Circus.fspro'
*/

using System;
using System.Collections.Generic;

namespace Audio
{
    public class AudioEvent
    {
        public static readonly FMOD.GUID Chain = new FMOD.GUID { Data1 = -640444492, Data2 = 1190958630, Data3 = 1990105474, Data4 = 1486807802 };
        public static readonly FMOD.GUID PlayerFootsteps = new FMOD.GUID { Data1 = -9758484, Data2 = 1245021375, Data3 = 1395276450, Data4 = 767652684 };
        public static readonly FMOD.GUID RoomAmbience = new FMOD.GUID { Data1 = 920241656, Data2 = 1177892146, Data3 = 1638935171, Data4 = -282666063 };


        public static readonly Dictionary<string, FMOD.GUID> AudioEventNameToGuid = new Dictionary<string, FMOD.GUID>()
        {
                {"Chain", Chain}, {"PlayerFootsteps", PlayerFootsteps}, {"RoomAmbience", RoomAmbience}, 
        };
    }

    public class AudioBus
    {
        public static readonly FMOD.GUID MasterBus = new FMOD.GUID { Data1 = -988115165, Data2 = 1228846850, Data3 = -1090998861, Data4 = 252002649 };
        public static readonly FMOD.GUID Reverb = new FMOD.GUID { Data1 = -1710510160, Data2 = 1140030815, Data3 = -632194644, Data4 = 2090800667 };


        public static readonly Dictionary<string, FMOD.GUID> AudioBusNameToGuid = new Dictionary<string, FMOD.GUID>()
        {
                {"MasterBus", MasterBus}, {"Reverb", Reverb}, 
        };
    }

    public class AudioBank
    {
        public static readonly FMOD.GUID Master = new FMOD.GUID { Data1 = -1450428026, Data2 = 1150626526, Data3 = -370190429, Data4 = -1375635118 };


        public static readonly Dictionary<string, FMOD.GUID> AudioBankNameToGuid = new Dictionary<string, FMOD.GUID>()
        {
                {"Master", Master}, 
        };
    }

}

