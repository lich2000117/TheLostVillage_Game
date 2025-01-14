﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevionGames
{
    [Icon("Component")]
    [ComponentMenu("Component/Set Enabled")]
    public class SetEnabled : Action
    {
        [SerializeField]
        private TargetType m_Target = TargetType.Player;
        [SerializeField]
        private string m_ComponentName=string.Empty;
        [SerializeField]
        private bool m_Enable=false;

        private Behaviour m_Component;

        public override void OnStart()
        {
            GameObject target = GetTarget(this.m_Target);
            this.m_Component = target.GetComponent(this.m_ComponentName) as Behaviour;
        }

        public override ActionStatus OnUpdate()
        {
            if (this.m_Component == null)
            {
                Debug.LogWarning("Missing Component of type "+this.m_ComponentName+"!");
                return ActionStatus.Failure;
            }
            this.m_Component.enabled = this.m_Enable;
            return ActionStatus.Success;
        }
    }
}