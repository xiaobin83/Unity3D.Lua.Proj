using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace lua
{
	internal class LuaStateMachineBehaviour : StateMachineBehaviour
	{
		bool inited = false;
		LuaBehaviour behaviour;

		void InitBehaviour(Animator animator)
		{
			if (inited) return;
			inited = true;
			behaviour = animator.gameObject.GetComponent<LuaBehaviour>();
		}

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			InitBehaviour(animator);
			if (behaviour != null)
				behaviour.SendLuaMessage(LuaBehaviour.Message.STA_OnEnter, animator, stateInfo, layerIndex);
		}

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			InitBehaviour(animator);
			if (behaviour != null)
				behaviour.SendLuaMessage(LuaBehaviour.Message.STA_OnExit, animator, stateInfo, layerIndex);
		}

		public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			InitBehaviour(animator);
			if (behaviour != null)
				behaviour.SendLuaMessage(LuaBehaviour.Message.STA_OnUpdate, animator, stateInfo, layerIndex);
		}
	}

}
