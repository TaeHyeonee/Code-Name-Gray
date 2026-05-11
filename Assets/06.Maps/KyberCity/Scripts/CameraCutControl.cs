using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ILranch
{
    public class CameraCutControl : MonoBehaviour
    {
        public enum WorkMode
        {
            lerp = 1,
            rotate = 2,
        }
        public WorkMode WorkModes = WorkMode.lerp;
        [Range(0, 50)]
        public float SpeedLerp = 0f;
        public bool Inversed;

        [Space(7)]
        public bool SetStart;
        public bool SetEnd;
        public bool AffectRotat = true;

        [Space(7)]
        public bool IsSmooth;
        public bool ResetAfterSmooth;

        public Animator[] AnimatorList;

        float _timeStartedLerping;
        bool _isLerping;
        float PrevSpeedLerp;
        Vector3 StartPos;
        Vector3 EndPos;
        Quaternion EndPosRot;

        GameObject StartPosGO;
        //GameObject EndPosGO;
        bool rotated;


        // Start is called before the first frame update
        void Start()
        {
            StartPosGO = new GameObject { };
            //StartPosGO.transform.parent = this.gameObject.transform;
            StartPosGO.transform.position = this.gameObject.transform.position;
            StartPosGO.transform.rotation = transform.rotation;
            StartPosGO.transform.name = "StartPosGO";

            //EndPosGO = new GameObject { };
            //EndPosGO.transform.parent = this.gameObject.transform;
            //EndPosGO.transform.position = this.gameObject.transform.position;
            //EndPosGO.transform.name = "EndPosGO";

            EndPos = transform.position;
            EndPosRot = transform.rotation;

            foreach (Animator anim in AnimatorList)
            {
                if (anim)
                {
                    anim.Play("State14", -1, 0);
                    anim.speed = 0;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (WorkModes == WorkMode.lerp)
            {
                if (rotated)
                {
                    _isLerping = false;
                    SpeedLerp = 0;
                    PrevSpeedLerp = 0;
                    rotated = false;
                    return;
                }

                if (_isLerping)
                {
                    if (!IsSmooth)
                    {
                        float timeSinceStarted = Time.time - _timeStartedLerping;
                        float percentageComplete = timeSinceStarted / (10 - SpeedLerp);
                        transform.position = Vector3.Lerp(StartPosGO.transform.position, EndPos, percentageComplete);
                        if (AffectRotat) transform.rotation = Quaternion.Lerp(StartPosGO.transform.rotation, EndPosRot, percentageComplete);
                        if (percentageComplete >= 1.0f)
                        {
                            _isLerping = false;
                            SpeedLerp = 0;
                            PrevSpeedLerp = 0;
                            foreach (Animator anim in AnimatorList)
                            {
                                //anim.Play("State14", -1, 0);
                                //anim.speed = 0;
                            }
                        }
                    }
                    else
                    {
                        transform.position = Vector3.Lerp(transform.position, EndPos, SpeedLerp * Time.deltaTime);
                        if (AffectRotat) transform.rotation = Quaternion.Lerp(transform.rotation, EndPosRot, SpeedLerp * Time.deltaTime);
                    }
                }

                if (ResetAfterSmooth)
                {
                    _isLerping = false;
                    SpeedLerp = 0;
                    PrevSpeedLerp = 0;
                    ResetAfterSmooth = false;
                    foreach (Animator anim in AnimatorList)
                    {
                        if (anim)
                        {
                            anim.Play("State14", -1, 0);
                            anim.speed = 0;
                        }
                    }
                }

                //
                if (SpeedLerp == 0 & PrevSpeedLerp != 0)
                {
                    _isLerping = false;
                    PrevSpeedLerp = 0;
                    foreach (Animator anim in AnimatorList)
                    {
                        if (anim)
                        {
                            anim.Rebind();
                            anim.Play("State14", -1, 0);
                            anim.speed = 0;
                        }
                    }
                }
                if (PrevSpeedLerp != SpeedLerp)
                {
                    if (PrevSpeedLerp == 0) StartLerping();
                    PrevSpeedLerp = SpeedLerp;
                    if (IsSmooth)
                    {
                        transform.position = StartPosGO.transform.position;
                        transform.rotation = StartPosGO.transform.rotation;
                    }
                    foreach (Animator anim in AnimatorList)
                    {
                        if (anim)
                        {
                            anim.Play("State14", -1, 0);
                            anim.speed = 1;
                        }
                    }
                }

                if (SetStart)
                {
                    StartPosGO.transform.position = transform.position;
                    StartPosGO.transform.rotation = transform.rotation;
                    SetStart = false;
                    Debug.Log("lerp start " + System.DateTime.Now);
                }
                if (SetEnd)
                {
                    EndPos = transform.position;
                    EndPosRot = transform.rotation;
                    SetEnd = false;
                    Debug.Log("lerp end " + System.DateTime.Now);
                }
            }
            else
            {
                rotated = true;
                if (!Inversed)
                {
                    transform.RotateAround(StartPosGO.transform.position, Vector3.up, SpeedLerp * 2 * Time.deltaTime);
                }
                else
                {
                    transform.RotateAround(StartPosGO.transform.position, Vector3.up, -SpeedLerp * 2 * Time.deltaTime);
                }
            }
        }

        void FixedUpdate()
        {

        }

        void StartLerping()
        {
            _isLerping = true;
            _timeStartedLerping = Time.time;
        }
    }
}
