using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat
{
    public class UnityAudioNode : UnityComponentNode
    {
        static byte[] COPY_BUFFER = new byte[8192];

        private AudioPlayerBase audioPlayer;
        private string cachePath;
        private string fieldName;
        private string audioPlayerName;
        private bool streamed;
        private bool arrayItem;
        private int samplesLength;
        private ResourceResponse resourceResponse;
        private int index;

        public UnityAudioNode(string name, PCFResourceType resourceType, UInt32 referenceID) : base(name, resourceType, referenceID)
        {
            this.resourceType = PCFResourceType.AUDIO;
        }

        public override void Deserialize(Dictionary<PCFResourceType, DataBlockBase> dataBlocks,
                        UnityNodeBase parentNode,
                        ResourceResponse resourceResponse,
                        List<Action<UnityNodeBase>> postInstallActions,
                        bool optimizedLoad)
        {
            if (!this.isDeserialized)
            {
                ResourceBlock dataBlock = dataBlocks[resourceType] as ResourceBlock;
                AssetResource resource = dataBlock.GetResource(this.referenceID);

                if (resource != null)
                {
                    this.resourceResponse = resourceResponse;

                    byte[] metaDataBuffer = resource.GetMetaData();
                    JObject metaData = JObject.Parse(System.Text.Encoding.UTF8.GetString(metaDataBuffer));

                    this.fieldName = metaData.Value<string>("fieldName");
                    this.audioPlayerName = metaData.Value<string>("name");
                    this.arrayItem = bool.Parse(metaData.Value<string>("arrayItem"));
                    this.samplesLength = metaData.Value<int>("samples");

                    //Lets us know if this audioclip should be streamed directly from the main pcf file.
                    this.streamed = bool.Parse(metaData.Value<string>("streamed"));

                    if (resource.IsStreamed())
                    {
                        UnityNodeBase current = parentNode;
                        while (current.ParentNode != null)
                        {
                            current = current.ParentNode;
                        }

                        IFileHandle filePath = null;
                        if (current is UnityRootNode)
                        {
                            UnityRootNode rootNode = current as UnityRootNode;
                            filePath = rootNode.GetFile();
                        }

                        if (filePath != null & filePath.Exists)
                        {
                            int streamPosition = (int)resource.GetStreamPosition();
                            int streamLength = (int)resource.GetStreamLength();

                            //Copy ogg file to temporary cache path for use.
                            this.cachePath = Application.temporaryCachePath + "/" + this.referenceID + ".ogg";

                            if (File.Exists(this.cachePath))
                            {
                                File.Delete(this.cachePath);
                            }

                            Stream streamedFile = filePath.GetFileStream(FileMode.Open);
                            FileStream oggOutput = new FileStream(this.cachePath, FileMode.Create, FileAccess.Write);

                            streamedFile.Seek(streamPosition, SeekOrigin.Begin);
                            int bytesLeft = streamLength;
                            int bytesWritten = 0;

                            while (bytesLeft > 0)
                            {
                                int bytesRead = streamedFile.Read(COPY_BUFFER, 0, Math.Min(bytesLeft, COPY_BUFFER.Length));
                                int bytesToWrite = COPY_BUFFER.Length;
                                
                                //Should happen for the last buffer we write.
                                if (bytesLeft < COPY_BUFFER.Length)
                                {
                                    bytesToWrite = bytesLeft;
                                }

                                oggOutput.Write(COPY_BUFFER, 0, bytesToWrite);

                                bytesWritten += bytesToWrite;
                                bytesLeft -= bytesRead;
                            }

                            oggOutput.Dispose();
                            oggOutput.Close();

                            streamedFile.Close();

                            if (bytesWritten != streamLength)
                            {
                                Debug.LogError("Missmatch in ogg cache file!");
                            }

                            if (streamed)
                            {
                                this.audioPlayer = new StreamedAudioPlayer(this.referenceID, this.audioPlayerName, this.cachePath, this.samplesLength);
                            }
                            else
                            {
                                this.audioPlayer = new BufferedAudioPlayer(this.referenceID, this.audioPlayerName, this.cachePath, this.samplesLength);
                            }
                        }
                    }

                    if (resourceResponse != null && this.audioPlayer != null)
                    {
                        if (arrayItem)
                        {
                            this.index = resourceResponse.GetFieldDeserializer.SetArrayItem(this.audioPlayer.GetAudioClip());
                        }
                        else
                        {
                            resourceResponse.GetFieldDeserializer.SetField(fieldName, this.audioPlayer.GetAudioClip());
                        }
                    }
                }

                this.isDeserialized = true;
            }
        }

        public override System.Object GetObject()
        {
            return this.audioPlayer.GetAudioClip();
        }

        public override void Destroy()
        {
            if (this.audioPlayer != null)
            {
                this.audioPlayer.Destroy();
            }

            if (File.Exists(this.cachePath))
            {
                File.Delete(this.cachePath);
            }

            base.Destroy();
        }

        public override void Reconstruct()
        {
            Debug.Log("reconstruct audio node");

            if (this.audioPlayer != null)
            {
                this.audioPlayer.Destroy();
            }

            if (streamed)
            {
                this.audioPlayer = new StreamedAudioPlayer(this.referenceID, this.audioPlayerName, this.cachePath, this.samplesLength);
            }
            else
            {
                this.audioPlayer = new BufferedAudioPlayer(this.referenceID, this.audioPlayerName, this.cachePath, this.samplesLength);
            }

            if (resourceResponse != null && this.audioPlayer != null)
            {
                if (arrayItem)
                {
                    FieldDeserializer field = resourceResponse.GetFieldDeserializer;
                    field.SetArrayItem(this.audioPlayer.GetAudioClip(), this.index);
                }
                else
                {
                    resourceResponse.GetFieldDeserializer.SetField(fieldName, this.audioPlayer.GetAudioClip());
                }
            }
        }
    }
}
