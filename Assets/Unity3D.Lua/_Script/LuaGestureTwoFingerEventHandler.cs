/*
MIT License

Copyright (c) 2016 xiaobin83

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace lua
{
    class LuaGestureTwoFingerEventHandler : LuaInstanceBehaviour0
    {
        public class SGestureTwoFingers
        {
            public Vector2 startDirection;
            public Vector2 direction;
            public Quaternion deltaRotation;

            public Vector2[] startPosition = new Vector2[2];
            public Vector2[] position = new Vector2[2];

            public float startTime;
            public float deltaTime;
        }
        public class TouchWrap
        {
            public Vector2 position;
            public Vector2 deltaPosition;
        }
        public class InputWrap
        {
            public static int touchCount;
            static TouchWrap[] touches;
            static Vector3 curTouch;
            public static TouchWrap GetTouch(int idx)
            {
                return touches[idx];
            }
            public static void Update()
            {
#if UNITY_EDITOR
                if (Input.GetKey(KeyCode.LeftAlt))
                {
                    //if(Input.GetMouseButton(0))
                    {
                        if (touches == null)
                        {
                            touches = new TouchWrap[2];
                        }
                        if (touches[0] == null)
                        {
                            touches[0] = new TouchWrap();
                            touches[0].position = Input.mousePosition;
                            curTouch = Input.mousePosition;
                            touchCount = 1;
                        }
                    }
                }
                else
                {
                    if(touches != null)
                    {
                        touches = null;
                        curTouch = Vector3.zero;
                        touchCount = 0;
                    }
                }

                if(Input.GetMouseButton(0) && 
                   touches != null)
                {
                    if(touches[1] == null)
                    {
                        touches[1] = new TouchWrap();
                    }
                    touches[1].position = Input.mousePosition;
                    touchCount = 2;
                }
#else
                touchCount = Input.touchCount;

                if (touchCount > 0 && (touches == null || touches.Length != touchCount))
                {
                    touches = new TouchWrap[touchCount];
                }
				if (touches != null)
				{
					for (int i = 0; i < touches.Length && i < Input.touchCount; i++)
					{
						if (touches[i] == null)
						{
							touches[i] = new TouchWrap();
						}
						touches[i].position = Input.GetTouch(i).position;
						touches[i].deltaPosition = Input.GetTouch(i).deltaPosition;
					}
				}
#endif
			}
        }

        SGestureTwoFingers g;
        void Update()
        {
            InputWrap.Update();
            if (InputWrap.touchCount == 2)
            {
                TouchWrap t0 = InputWrap.GetTouch(0);
                TouchWrap t1 = InputWrap.GetTouch(1);
                if (g == null)
                {
                    g = new SGestureTwoFingers();
                    g.startPosition[0] = t0.position;
                    g.startPosition[1] = t1.position;
                    g.startTime = Time.realtimeSinceStartup;
                    g.startDirection = t1.position - t0.position;
                    OnGestureTwoFingerEventBegin(g);
                }
                g.direction = t1.position - t0.position;
                Vector2 t0PrevPos = t0.position - t0.deltaPosition;
                Vector2 t1PrevPos = t1.position - t1.deltaPosition;
                Vector2 prevDir = t1PrevPos - t0PrevPos;
                g.deltaRotation = Quaternion.FromToRotation(prevDir, g.direction);
                g.deltaTime = Time.realtimeSinceStartup - g.startTime;
                g.position[0] = t0.position;
                g.position[1] = t1.position;
                OnGestureTwoFingerEventMove(g);
            }
            else
            {
                if (g != null)
                {
                    OnGestureTwoFingerEventEnd(g);
                    g = null;
                }
            }
        }
        public void OnGestureTwoFingerEventBegin(SGestureTwoFingers eventData)
        {
            luaBehaviour.SendLuaMessage(LuaBehaviour.Message.Event_GestureTwoFingerBegin, eventData);
        }
        public void OnGestureTwoFingerEventMove(SGestureTwoFingers eventData)
        {
            luaBehaviour.SendLuaMessage(LuaBehaviour.Message.Event_GestureTwoFingerMove, eventData);
        }
        public void OnGestureTwoFingerEventEnd(SGestureTwoFingers eventData)
        {
            luaBehaviour.SendLuaMessage(LuaBehaviour.Message.Event_GestureTwoFingerEnd, eventData);
        }
    }
}
