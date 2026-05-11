using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILranch
{
    public class KCCarInflate : MonoBehaviour
    {
        [Header("press SPACE to start")]
        public float speed = 0.03f;
        public GameObject wholeentity;
        public GameObject car;
        public GameObject[] wheels;

        float ValueInflate = 100;
        float wholeentitymove = 0.137f;
        float carpos;
        bool start;

        // Start is called before the first frame update
        void Start()
        {
            carpos = car.transform.localPosition.y;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                start = true;
            }

            if(start)
            {
                wholeentitymove = Mathf.Lerp(wholeentitymove, 0.2f, speed * Time.deltaTime);
                wholeentity.transform.position = new Vector3(wholeentity.transform.position.x, wholeentitymove, wholeentity.transform.position.z);

                carpos = Mathf.Lerp(carpos, 0, speed * Time.deltaTime);
                car.transform.localPosition = new Vector3(car.transform.localPosition.x, carpos, car.transform.localPosition.z);

                ValueInflate = Mathf.Lerp(ValueInflate, 0, speed * Time.deltaTime);
                for (int k0 = 0; k0 < wheels.Length; k0++)
                {
                    wheels[k0].GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, ValueInflate);
                }
            }
        }
    }
}
