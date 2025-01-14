﻿using UnityEngine;

namespace DevionGames
{
    [Icon(typeof(Animator))]
    [ComponentMenu("Animator/Set Bool")]
    public class SetBool: Action
    {
        [SerializeField]
        private TargetType m_Target = TargetType.Player;
        [SerializeField]
        private string m_ParameterName = string.Empty;
        [SerializeField]
        private bool m_Value = true;

        private Animator m_Animator;

        public override void OnStart()
        {
            this.m_Animator = this.m_Target == TargetType.Self ? gameObject.GetComponent<Animator>() : playerInfo.animator;
        }

        public override ActionStatus OnUpdate()
        {
            if (m_Animator == null)
            {
                Debug.LogWarning("Missing Component of type Animator!");
                return ActionStatus.Failure;
            }
            this.m_Animator.SetBool(this.m_ParameterName, this.m_Value);

            return ActionStatus.Success;
        }
    }
}
