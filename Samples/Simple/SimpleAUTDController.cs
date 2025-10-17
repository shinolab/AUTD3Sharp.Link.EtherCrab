using System;
using AUTD3Sharp;
using AUTD3Sharp.Gain;
using AUTD3Sharp.Modulation;
using AUTD3Sharp.Link;
using UnityEngine;
using static AUTD3Sharp.Units;

#if UNITY_2020_2_OR_NEWER
#nullable enable
#endif

public class SimpleAUTDController : MonoBehaviour
{
    private Controller? _autd = null;
    public GameObject? Target = null;

    private Vector3 _oldPosition;

    private static bool _isPlaying = true;

    private void Awake()
    {
        try
        {
            _autd = Controller.Open(new[] { new AUTD3(pos: gameObject.transform.position, rot: gameObject.transform.rotation) },
                new EtherCrab((idx, status) =>
                    {
                        UnityEngine.Debug.LogError($"Device[{idx}]: {status}");
                    }, new EtherCrabOption()));
        }
        catch (Exception)
        {
            UnityEngine.Debug.LogError("Failed to open AUTD3 controller!");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
            UnityEngine.Application.Quit();
#endif
        }

        _autd!.Send(new Sine(freq: 150 * Hz, option: new SineOption()));

        if (Target == null) return;
        _autd!.Send(new Focus(pos: Target.transform.position, option: new FocusOption()));
        _oldPosition = Target.transform.position;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!_isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
#endif
        if (_autd == null) return;

        if (Target == null || Target.transform.position == _oldPosition) return;
        _autd!.Send(new Focus(pos: Target.transform.position, option: new FocusOption()));
        _oldPosition = Target.transform.position;
    }

    private void OnApplicationQuit()
    {
        _autd?.Dispose();
    }
}

#if UNITY_2020_2_OR_NEWER
#nullable restore
#endif
