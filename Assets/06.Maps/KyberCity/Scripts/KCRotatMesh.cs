using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILranch
{
    public class KCRotatMesh : MonoBehaviour
    {
        public float SpeedRotate = 550;
        bool Mode;

        void Awake()
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks * 1000);
        }

        // Start is called before the first frame update
        void Start()
        {
            if (UnityEngine.Random.Range(0, 100) < 50)
            {
                Mode = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Mode)
            {
                transform.Rotate(0, 0, SpeedRotate * Time.deltaTime);
            }
            else
            {
                transform.Rotate(0, 0, -SpeedRotate * Time.deltaTime);
            }
        }
    }
}
