﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DevionGames
{
    public abstract class BaseTrigger : CallbackHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        //Player GameObject, overrride this and set 
        public abstract PlayerInfo PlayerInfo { get; }

        //Callbacks for scene reference use.
        public override string[] Callbacks
        {
            get
            {
                return new[] {
                    "OnPointerEnter",
                    "OnPointerExit",
                    "OnTriggerUsed",
                    "OnTriggerUnUsed",
                    "OnCameInRange",
                    "OnWentOutOfRange",
                };
            }
        }

        //The maximum distance for trigger useage
        public float useDistance = 1.2f;
        [EnumFlags]
        public TriggerInputType triggerType = TriggerInputType.OnClick | TriggerInputType.Key;
        //If in range and trigger input type includes key, the key to use the trigger.
        public KeyCode key = KeyCode.F;

        //Does the user quits application
        private bool m_ApplicationQuit;
        //Custom Trigger callbacks
        protected ITriggerEventHandler[] m_TriggerEvents;
        //Current trigger used by the player
        public static BaseTrigger currentUsedTrigger;


        //All triggers in range
        private static List<BaseTrigger> m_TriggerInRange = new List<BaseTrigger>();
        //Dictionary of callbacks
        protected Dictionary<Type, string> m_CallbackHandlers;

        protected delegate void EventFunction<T>(T handler, GameObject player);
        protected delegate void PointerEventFunction<T>(T handler, PointerEventData eventData);

        //Is the player in range, set by OnTriggerEnter/OnTriggerExit or if trigger is attached to player in Start?
        private bool m_InRange;
        public bool InRange
        {
            get
            {
                return this.m_InRange;
            }
            protected set
            {
                if (this.m_InRange != value)
                {
                    this.m_InRange = value;
                    if (this.m_InRange){
                        NotifyCameInRange();
                    }else{
                        NotifyWentOutOfRange();
                    }
                }
            }
        }

        //Is the trigger currently in use?
        private bool m_InUse;
        public bool InUse
        {
            get { return this.m_InUse; }
            protected set
            {
                if (this.m_InUse != value)
                {
                    this.m_InUse = value;
                    if (!this.m_InUse){
                        NotifyUnUsed();
                    }else{
                        NotifyUsed();
                    }

                }
            }
        }

        protected virtual void Start()
        {
            this.RegisterCallbacks();
            this.m_TriggerEvents = GetComponentsInChildren<ITriggerEventHandler>();
            if (gameObject == PlayerInfo.gameObject || this.useDistance == -1) {
                InRange = true;
            }
            else{
                //Create trigger collider
                CreateTriggerCollider();
            }
        }

        protected virtual void Update() {
            if (!InRange) { return; }

            //Check for key down and if trigger input type supports key.
            if (Input.GetKeyDown(key) && triggerType.HasFlag<TriggerInputType>(TriggerInputType.Key) && InRange && IsBestTrigger()){
                Use();
            }
        }

        //Called when the player quits the game
        private void OnApplicationQuit()
        {
            this.m_ApplicationQuit = true;
        }

        protected virtual void OnDestroy()
        {
            //Check if the user quits the game
            if (!this.m_ApplicationQuit)
            {
                //Set in range to false when the game object gets destroyed to invoke OnTriggerUnUsed events
                InRange = false;
            }
        }

        //OnTriggerEnter is called when the Collider other enters the trigger.
        protected virtual void OnTriggerEnter(Collider other)
        {
            //Check if the collider other is player 
            if (other.tag == PlayerInfo.gameObject.tag)
            {
                //Set that player is in range
                InRange = true;
            }
        }

        //OnTriggerExit is called when the Collider other has stopped touching the trigger.
        protected virtual void OnTriggerExit(Collider other)
        {
            //Check if the collider other is player
            if (other.tag == PlayerInfo.gameObject.tag)
            {
                //Set that player is out of range
                InRange = false;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            //Check if  TriggerInputType.OnClick is included.
            if (triggerType.HasFlag<TriggerInputType>(TriggerInputType.OnClick) && !UnityTools.IsPointerOverUI())
            {
                Use();
            }
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (!UnityTools.IsPointerOverUI())
            {
                ExecuteEvent<ITriggerPointerEnter>(Execute, eventData);
            }
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (!UnityTools.IsPointerOverUI())
            {
                ExecuteEvent<ITriggerPointerExit>(Execute, eventData);
            }
        }

        //Use the trigger
        public virtual bool Use()
        {
            //Can the trigger be used?
            if (!CanUse())
            {
                return false;
            }
          //  BaseTrigger.currentUsedTrigger = this;
            //Set the trigger in use
            this.InUse = true;
            return true;
        }

        //Can the trigger be used?
        public virtual bool CanUse()
        {
            //Return false if the trigger is already used
            if (InUse || (BaseTrigger.currentUsedTrigger != null && BaseTrigger.currentUsedTrigger.InUse))
            {
                DisplayInUse();
                return false;
            }

            if (this.useDistance == -1) { return true; }

            //Return false if the player is not in range
            if (!InRange)
            {
                DisplayOutOfRange();
                return false;
            }

            Vector3 targetPosition = UnityTools.GetBounds(gameObject).center;
            Vector3 playerPosition = PlayerInfo.transform.position;
            Bounds bounds = PlayerInfo.bounds;
            playerPosition.y += bounds.center.y + bounds.extents.y;
            Vector3 direction = targetPosition - playerPosition;
            Collider collider = PlayerInfo.collider;
            collider.enabled = false;
            RaycastHit hit;
            bool raycast = Physics.Raycast(playerPosition, direction, out hit);
            collider.enabled = true;
            if (raycast && hit.transform != transform && gameObject != PlayerInfo.gameObject)
            {
                return false;
            }

            //Trigger can be used
            return true;
        }

        protected virtual void OnWentOutOfRange() { }

        private void NotifyWentOutOfRange(){
            ExecuteEvent<ITriggerWentOutOfRange>(Execute, true);
            BaseTrigger.m_TriggerInRange.Remove(this);
            this.InUse = false;
            OnWentOutOfRange();
        }

        protected virtual void OnCameInRange() { }

        private void NotifyCameInRange() {
            ExecuteEvent<ITriggerCameInRange>(Execute, true);
            BaseTrigger.m_TriggerInRange.Add(this);
            //InputTriggerType.OnTriggerEnter is supported
            if (triggerType.HasFlag<TriggerInputType>(TriggerInputType.OnTriggerEnter) && IsBestTrigger()){
                Use();
            }
            OnCameInRange();
        }

        protected virtual void OnTriggerUsed() { }

        private void NotifyUsed() {
            BaseTrigger.currentUsedTrigger = this;
            ExecuteEvent<ITriggerUsedHandler>(Execute);
            OnTriggerUsed();
        }

        protected virtual void OnTriggerUnUsed() { }

        private void NotifyUnUsed() {
            ExecuteEvent<ITriggerUnUsedHandler>(Execute, true);
            BaseTrigger.currentUsedTrigger = null;
            OnTriggerUnUsed();
        }

        //Notify player that he is already using a trigger.
        protected virtual void DisplayInUse() { }

        //Notify player that he is out of range
        protected virtual void DisplayOutOfRange() { }

        //Creates a sphere collider so OnTriggerEnter/OnTriggerExit gets called
        protected virtual void CreateTriggerCollider()
        {
            Vector3 position = Vector3.zero;

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                position = collider.bounds.center;
                position.y = (collider.bounds.center - collider.bounds.extents).y;
                position = transform.InverseTransformPoint(position);
            }

            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.center = position;
            Vector3 scale = transform.lossyScale;
            sphereCollider.radius = useDistance / Mathf.Max(scale.x, scale.y, scale.z);
        }

        /*Returns true if this is the best trigger. Used for TriggerInputType.Key and TriggerInputType.OnTriggerEnter
          Calculated based on distance and rotation of the player to the trigger.*/
        protected virtual bool IsBestTrigger()
        {
            if (gameObject == PlayerInfo.gameObject)
            {
                return true;
            }

            BaseTrigger tMin = null;
            float minDist = Mathf.Infinity;
            Vector3 currentPos = PlayerInfo.transform.position;
            foreach (BaseTrigger t in BaseTrigger.m_TriggerInRange)
            {
                if (t.key != key) continue;
                Vector3 dir = t.transform.position - currentPos;
                float angle = 0f;
                if (dir != Vector3.zero)
                   angle = Quaternion.Angle(PlayerInfo.transform.rotation, Quaternion.LookRotation(dir));

                float dist = Vector3.Distance(t.transform.position, currentPos) * angle;
                if (dist < minDist)
                {
                    tMin = t;
                    minDist = dist;
                }
            }
            return tMin == this;
        }

        protected static void Execute(ITriggerUsedHandler handler, GameObject player)
        {
            handler.OnTriggerUsed(player);
        }

        protected static void Execute(ITriggerUnUsedHandler handler, GameObject player)
        {
            handler.OnTriggerUnUsed(player);
        }

        protected static void Execute(ITriggerCameInRange handler, GameObject player)
        {
            handler.OnCameInRange(player);
        }

        protected static void Execute(ITriggerWentOutOfRange handler, GameObject player)
        {
            handler.OnWentOutOfRange(player);
        }

        protected static void Execute(ITriggerPointerEnter handler, PointerEventData eventData)
        {
            handler.OnPointerEnter(eventData);
        }

        protected static void Execute(ITriggerPointerExit handler, PointerEventData eventData)
        {
            handler.OnPointerExit(eventData);
        }

        //Execute event
        protected void ExecuteEvent<T>(EventFunction<T> func, bool includeDisabled = false) where T : ITriggerEventHandler
        {
            for (int i = 0; i < this.m_TriggerEvents.Length; i++)
            {
                ITriggerEventHandler handler = this.m_TriggerEvents[i];
                if (ShouldSendEvent<T>(handler, includeDisabled))
                {
                    func.Invoke((T)handler, PlayerInfo.gameObject);
                }
            }
            string eventID = string.Empty;
            if (this.m_CallbackHandlers.TryGetValue(typeof(T), out eventID))
            {
                CallbackEventData triggerEventData = new CallbackEventData();
                triggerEventData.AddData("Trigger", this);
                triggerEventData.AddData("Player", PlayerInfo.gameObject);
                triggerEventData.AddData("EventData", new PointerEventData(EventSystem.current));
                base.Execute(eventID, triggerEventData);
            }
        }

        protected void ExecuteEvent<T>(PointerEventFunction<T> func, PointerEventData eventData, bool includeDisabled = false) where T : ITriggerEventHandler
        {
            for (int i = 0; i < this.m_TriggerEvents.Length; i++)
            {
                ITriggerEventHandler handler = this.m_TriggerEvents[i];
                if (ShouldSendEvent<T>(handler, includeDisabled))
                {
                    func.Invoke((T)handler, eventData);
                }
            }

            string eventID = string.Empty;
            if (this.m_CallbackHandlers.TryGetValue(typeof(T), out eventID))
            {
                CallbackEventData triggerEventData = new CallbackEventData();
                triggerEventData.AddData("Trigger", this);
                triggerEventData.AddData("Player", PlayerInfo.gameObject);
                triggerEventData.AddData("EventData", new PointerEventData(EventSystem.current));
                base.Execute(eventID, triggerEventData);
            }
        }

        //Check if we should execute the event on that handler
        protected bool ShouldSendEvent<T>(ITriggerEventHandler handler, bool includeDisabled)
        {
            var valid = handler is T;
            if (!valid)
                return false;
            var behaviour = handler as Behaviour;
            if (behaviour != null && !includeDisabled)
                return behaviour.isActiveAndEnabled;

            return true;
        }

        //TODO: Auto registration
        protected virtual void RegisterCallbacks()
        {
            this.m_CallbackHandlers = new Dictionary<Type, string>();
            this.m_CallbackHandlers.Add(typeof(ITriggerPointerEnter), "OnPointerEnter");
            this.m_CallbackHandlers.Add(typeof(ITriggerPointerExit), "OnPointerExit");
            this.m_CallbackHandlers.Add(typeof(ITriggerUsedHandler), "OnTriggerUsed");
            this.m_CallbackHandlers.Add(typeof(ITriggerUnUsedHandler), "OnTriggerUnUsed");
            this.m_CallbackHandlers.Add(typeof(ITriggerCameInRange), "OnCameInRange");
            this.m_CallbackHandlers.Add(typeof(ITriggerWentOutOfRange), "OnWentOutOfRange");

        }

        [System.Flags]
        public enum TriggerInputType
        {
            OnClick = 1,
            Key = 2,
            OnTriggerEnter = 4
        }
    }
}