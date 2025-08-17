using System;
using Audio;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class FmodParameter {
    public const string FLEEING_ENEMIES = "FleeingEnemies";
    public const string PLAYER_STATE = "PlayerState";
    public const string LOCAL_ENEMY_STATE = "EnemyState";
    public const string GOAT_KILL_PROGRESS = "GoatKillProgress";
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start() {
        Play2DAudio(AudioEvent.RoomAmbience);
        Play3DAudio(AudioEvent.Music, Vector3.zero);
    }

    public EventInstance Play3DAudio(FMOD.GUID audioEvent, Transform target) {
        var instance = FMODUnity.RuntimeManager.CreateInstance(audioEvent);
        instance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(target));
        FMODUnity.RuntimeManager.AttachInstanceToGameObject(instance, target.gameObject);
        instance.start();
        return instance;
    }
    
    public EventInstance Play3DAudio(FMOD.GUID audioEvent, Vector3 position) {
        var instance = FMODUnity.RuntimeManager.CreateInstance(audioEvent);
        instance.set3DAttributes(position.To3DAttributes());
        instance.start();
        return instance;
    }
    
    public EventInstance Play2DAudio(FMOD.GUID audioEvent) {
        var instance = FMODUnity.RuntimeManager.CreateInstance(audioEvent);
        instance.start();
        return instance;
    }

    public void StopAudio(EventInstance instance) {
        if (instance.isValid()) {
            instance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
    
    public void SetGlobalParameter(string fmodParameter, float value) {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName(fmodParameter, value);
    }

    public void SetLocalParameter(EventInstance fmodEvent,string fmodParameter, float value) {
        if (!fmodEvent.isValid()) {
            return;
        }

        fmodEvent.setParameterByName(fmodParameter, value);
    }
}