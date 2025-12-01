using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DialogueSystem
{
    [CreateAssetMenu(fileName = "New actor", menuName = "Dialogue System/Create Actor")]
    public class Actor : ScriptableObject
    {
        [Header("Actor data")]
        public string actorName;
        public GameObject actorModel;

        [Header("Render position")]
        public Vector3 renderVector3Pose = Vector3.zero;
        public Vector3 renderVector3Rotation = Vector3.zero;
        public Vector3 renderVector3Scale = Vector3.one;

        public List<ActorPose> poses = new();

        public AnimationClip GetPoseClip(string poseName)
        {
            var matchingEntry = poses.FirstOrDefault(entry => entry.name == poseName);
            if (matchingEntry != null)
            {
                return matchingEntry.clip;
            }

            Debug.LogWarning($"Pose '{poseName}' not found for actor {actorName}.");
            return null;
        }
    }
}