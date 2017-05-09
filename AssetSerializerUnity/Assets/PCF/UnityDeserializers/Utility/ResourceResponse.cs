using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using PCFFileFormat.Serialization;

namespace PCFFileFormat
{
    public class ResourceResponse
    {
        private UInt32 referenceID;
        private ResourceResponse previous;
        private ResourceResponse next;
        private Action<ResourceResponse> completionHandler;

        //One request variable for each static type.
        private Texture2D textureRequest;
        private byte[] byteRequest;
        private Avatar avatarRequest;
        private AssetBundle assetBundleRequest;
        private Material materialRequest;
        private AudioClip audioClipRequest;
        private FieldDeserializer fieldDeserializer;
        private Dictionary<UInt32, UnityNodeBase> referencedNodes;

        public Texture2D GetTextureRequest
        {
            get { return this.textureRequest;  }
        }
        public byte[] GetByteRequest
        {
            get { return this.byteRequest; }
        }
        public Avatar GetAvatarRequest
        {
            get { return this.avatarRequest; }
        }
        public AssetBundle GetAssetBundleRequest
        {
            get { return this.assetBundleRequest; }
        }
        public Material GetMaterialRequest
        {
            get { return this.materialRequest; }
        }
        public AudioClip GetAudioClipRequest
        {
            get { return this.audioClipRequest; }
        }
        public FieldDeserializer GetFieldDeserializer
        {
            get { return this.fieldDeserializer; }
        }
        public Dictionary<UInt32, UnityNodeBase> GetReferencedNodes
        {
            get { return this.referencedNodes; }
        }

        public ResourceResponse(UInt32 referenceID, Action<ResourceResponse> completionHandler)
        {
            this.referenceID = referenceID;
            this.completionHandler = completionHandler;
        }

        public ResourceResponse()
        {

        }

        public void SetFieldDeserializer(FieldDeserializer fieldDeserializer)
        {
            this.fieldDeserializer = fieldDeserializer;
        }

        public void SetReferencedNodes(Dictionary<UInt32, UnityNodeBase> referencedNodes)
        {
            this.referencedNodes = referencedNodes;
        }

        public ResourceResponse GetPreviousResponse()
        {
            return this.previous;
        }

        public void SetPreviousResponse(ResourceResponse previous)
        {
            this.previous = previous;
        }

        public void SetNextResponse(ResourceResponse next)
        {
            this.next = next;
        }

        public ResourceResponse CanHandle(UInt32 nodeReferenceID)
        {
            if (this.referenceID == nodeReferenceID)
            {
                return this;
            }
            else
            {
                if (next != null)
                {
                    ResourceResponse foundHandler = next.CanHandle(nodeReferenceID);

                    if (foundHandler != null)
                        return foundHandler;
                }
            }

            return null;
        }

        public void HandleTextureResponse(Texture2D texture)
        {
            this.textureRequest = texture;

            Handle();
        }

        public void HandleMaterialResponse(Material material)
        {
            this.materialRequest = material;

            Handle();
        }

        public void HandleByteResponse(byte[] bytes)
        {
            this.byteRequest = bytes;

            Handle();
        }

        public void HandleAvatarResponse(Avatar avatar)
        {
            this.avatarRequest = avatar;

            Handle();
        }

        public void HandleAssetBundleResponse(AssetBundle assetBundle)
        {
            this.assetBundleRequest = assetBundle;

            Handle();
        }

        public void HandleAudioClipResponse(AudioClip audioClip)
        {
            this.audioClipRequest = audioClip;

            Handle();
        }

        //Should only be called by an explicit handler.
        void Handle()
        {
            if (this.completionHandler != null)
                this.completionHandler(this);
        }
    }
}
