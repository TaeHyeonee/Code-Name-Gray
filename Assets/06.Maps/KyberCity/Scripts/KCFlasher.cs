using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILranch
{
    //IL.ranch, 2025. ILonion32@gmail.com
    public class KCFlasher : MonoBehaviour
    {
        public bool IsActive;
        [ColorUsageAttribute(true, true)]
        public Color MaxBright;
        public float BrightBoost = 1;
        public float SwitchTime = 0.1f;
        public enum SwitchMode
        {
            simple = 0,
            multi = 1,
            broken = 2,
        }
        public SwitchMode SwitchModes = SwitchMode.simple;
        public int MatId = 0;
        public string ShaderString = "_EmissionColor"; //replace with _EmissiveColor for HDRP
        int mode;
        int count;
        Material _Material;

        void Awake()
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks * 1000);
        }

        // Use this for initialization
        void Start()
        {
            if (IsActive)
            {
                float random = UnityEngine.Random.Range(0.07f, 1.95f);
                InvokeRepeating("ColorSwitch", random, SwitchTime);
                _Material = GetComponent<Renderer>().materials[MatId];
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        void ColorSwitch()
        {
            //0 - 1 - 0 - 1 - 0...
            if (SwitchModes == SwitchMode.simple)
            {
                mode = 1 - mode;
                if (mode == 0)
                {
                    _Material.SetColor(ShaderString, Color.black);
                }
                else
                {
                    _Material.SetColor(ShaderString, MaxBright * BrightBoost);
                }
            }

            //0 - 0.5 - 1 - 0...
            else if (SwitchModes == SwitchMode.multi)
            {
                mode++;
                if (mode > 2) mode = 0;
                if (mode == 0)
                {
                    _Material.SetColor(ShaderString, Color.black);
                }
                else if (mode == 1)
                {
                    _Material.SetColor(ShaderString, (MaxBright * 0.5f) * BrightBoost);
                }
                else
                {
                    _Material.SetColor(ShaderString, MaxBright * BrightBoost);
                }
            }

            //broken lamp
            else
            {
                if (count < 10)
                {
                    mode++;
                    if (mode > 2) mode = 0;
                    if (mode == 0)
                    {
                        _Material.SetColor(ShaderString, Color.black);
                    }
                    else if (mode == 1)
                    {
                        _Material.SetColor(ShaderString, (MaxBright * 0.5f) * BrightBoost);
                    }
                    else
                    {
                        _Material.SetColor(ShaderString, MaxBright * BrightBoost);
                    }
                }
                else if (count < 20)
                {
                    _Material.SetColor(ShaderString, MaxBright * 0.5f);
                }
                else
                {
                    _Material.SetColor(ShaderString, MaxBright * BrightBoost);
                }
                count++;
                if (count > 50) count = 0;
            }
        }
    }
}
