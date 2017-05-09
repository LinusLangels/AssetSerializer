using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    public class AnimationClipUtils
    {
        private static Dictionary<SerializedAnimationChannelName, string> AnimationClipChannelNames = new Dictionary<SerializedAnimationChannelName, string> {
        { SerializedAnimationChannelName.TranslateX, "m_LocalPosition.x" },
        { SerializedAnimationChannelName.TranslateY, "m_LocalPosition.y" },
        { SerializedAnimationChannelName.TranslateZ, "m_LocalPosition.z" },
        { SerializedAnimationChannelName.RotateX, "m_LocalRotation.x" },
        { SerializedAnimationChannelName.RotateY, "m_LocalRotation.y" },
        { SerializedAnimationChannelName.RotateZ, "m_LocalRotation.z" },
        { SerializedAnimationChannelName.RotateW, "m_LocalRotation.w" },
        { SerializedAnimationChannelName.ScaleX, "m_LocalScale.x" },
        { SerializedAnimationChannelName.ScaleY, "m_LocalScale.y" },
        { SerializedAnimationChannelName.ScaleZ, "m_LocalScale.z" }
        };

        public static string GetAnimationClipChannelName(SerializedAnimationChannelName channel)
        {
            return AnimationClipChannelNames[channel];
        }

        public static SerializedAnimationChannelName GetAnimationClipChannelName(string channel)
        {
            foreach (KeyValuePair<SerializedAnimationChannelName, string> pair in AnimationClipChannelNames)
            {
                if (pair.Value == channel)
                {
                    return pair.Key;
                }
            }

            return SerializedAnimationChannelName.TranslateX;
        }
    }
}
