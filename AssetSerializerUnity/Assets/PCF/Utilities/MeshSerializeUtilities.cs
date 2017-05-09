using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace PCFFileFormat
{
	public class MeshSerializeUtilities 
	{
		public struct MeshInstance
		{
			public Mesh mesh;        
			public Matrix4x4 transform;
		}

		public static Mesh Duplicate (Mesh original)
		{
			Mesh duplicate = new Mesh();

			duplicate.vertices = original.vertices;
			duplicate.triangles = original.triangles;
			duplicate.uv = original.uv;
			duplicate.normals = original.normals;
			duplicate.tangents = original.tangents;
			duplicate.bindposes = original.bindposes;
			duplicate.boneWeights = original.boneWeights;

			return duplicate;
		}
		
		public static Mesh Combine (MeshInstance[] combines)
		{
			int vertexCount = 0;
			int triangleCount = 0;
			int offset = 0;
			int triangleOffset = 0;
			
			for (int i = 0; i < combines.Length; i++)
			{
				MeshInstance combine = combines[i];

				if (combine.mesh)
				{
					vertexCount += combine.mesh.vertexCount;
					triangleCount += combine.mesh.triangles.Length;
				}
			}

			//Precache size for all arrays, no need to use generic lists, more optimized.
			Vector3[] vertices = new Vector3[vertexCount];
			int[] triangles = new int[triangleCount];
			Vector3[] normals = new Vector3[vertexCount];
			Vector4[] tangents = new Vector4[vertexCount];
			Vector2[] uv = new Vector2[vertexCount];

			for (int i = 0; i < combines.Length; i++)
			{
				MeshInstance combine = combines[i];

				if (combine.mesh)
				{
					//Copy Verts and UVs
					CopyVerts(combine.mesh.vertices, vertices, offset, combine.transform);	
					CopyUV(combine.mesh.uv, uv, offset);

					//Copy Normals and Tangents
					Matrix4x4 invTranspose = combine.transform;
					invTranspose = invTranspose.inverse.transpose;
					CopyNormal(combine.mesh.normals, normals, offset, invTranspose);
					CopyTangents(combine.mesh.tangents, tangents, offset, invTranspose);

					//Copy Triangles
					int[] triangleArray = combine.mesh.triangles;
					for (int j = 0; j < triangleArray.Length; j++)
						triangles[triangleOffset++] = triangleArray[j] + offset;	

					offset += combine.mesh.vertexCount;
				}
			}		
			
			Mesh mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.normals = normals;
			mesh.uv = uv;
			mesh.tangents = tangents;
			mesh.name = "Combined Character";
			
			return mesh;
		}
		
		static void CopyVerts (Vector3[] src, Vector3[] dst, int offset, Matrix4x4 transform)
		{
			for (int i=0;i<src.Length;i++)
			{
				dst[i+offset] = transform.MultiplyPoint(src[i]);
			}
		}
		
		static void CopyNormal (Vector3[] src, Vector3[] dst, int offset, Matrix4x4 transform)
		{
			for (int i=0;i<src.Length;i++)
				dst[i+offset] = transform.MultiplyVector(src[i]).normalized;
		}
		
		static void CopyUV (Vector2[] src, Vector2[] dst, int offset)
		{
			for (int i=0;i<src.Length;i++)
				dst[i+offset] = src[i];
		}
		
		static void CopyTangents (Vector4[] src, Vector4[] dst, int offset, Matrix4x4 transform)
		{
			for (int i=0;i<src.Length;i++)
			{
				Vector4 p4 = src[i];
				Vector3 p = new Vector3(p4.x, p4.y, p4.z);
				p = transform.MultiplyVector(p).normalized;
				dst[i+offset] = new Vector4(p.x, p.y, p.z, p4.w);
			}
		}
		
		static void ReadVector3Array16bit (Vector3[] arr, BinaryReader buf)
		{
			var n = arr.Length;
			if (n == 0)
				return;
			
			// Read bounding box
			Vector3 bmin;
			Vector3 bmax;
			bmin.x = buf.ReadSingle ();
			bmax.x = buf.ReadSingle ();
			bmin.y = buf.ReadSingle ();
			bmax.y = buf.ReadSingle ();
			bmin.z = buf.ReadSingle ();
			bmax.z = buf.ReadSingle ();

	        byte[] bytes = buf.ReadBytes(n * 3 * 2);
	        byte[] shortBuffer = new byte[2];

	        // Decode vectors as 16 bit integer components between the bounds
	        // NOTE: Watch out for endianness bugs here since we use a very low level approach to create unsigned shorts.
	        int stride = 0;
	        for (int i = 0; i < n; ++i)
	        {
	            shortBuffer[0] = bytes[stride];
	            stride++;
	            shortBuffer[1] = bytes[stride];
	            stride++;
	            System.UInt16 ix = (ushort)(shortBuffer[0] | (shortBuffer[1] << 8));

	            shortBuffer[0] = bytes[stride];
	            stride++;
	            shortBuffer[1] = bytes[stride];
	            stride++;
	            System.UInt16 iy = (ushort)(shortBuffer[0] | (shortBuffer[1] << 8));

	            shortBuffer[0] = bytes[stride];
	            stride++;
	            shortBuffer[1] = bytes[stride];
	            stride++;
	            System.UInt16 iz = (ushort)(shortBuffer[0] | (shortBuffer[1] << 8));

	            float xx = ix / 65535.0f * (bmax.x - bmin.x) + bmin.x;
	            float yy = iy / 65535.0f * (bmax.y - bmin.y) + bmin.y;
	            float zz = iz / 65535.0f * (bmax.z - bmin.z) + bmin.z;
	            arr[i] = new Vector3(xx, yy, zz);
	        }
	    }

		static void WriteVector3Array16bit (Vector3[] arr, BinaryWriter buf)
		{
			if (arr.Length == 0)
				return;
			
			// Calculate bounding box of the array
			Bounds bounds = new Bounds (arr[0], new Vector3(0.001f,0.001f,0.001f));
			foreach (Vector3 v in arr)
				bounds.Encapsulate (v);
			
			// Write bounds to stream
			var bmin = bounds.min;
			var bmax = bounds.max;
			buf.Write (bmin.x);
			buf.Write (bmax.x);
			buf.Write (bmin.y);
			buf.Write (bmax.y);
			buf.Write (bmin.z);
			buf.Write (bmax.z);
			
			// Encode vectors as 16 bit integer components between the bounds
			foreach (Vector3 v in arr) {
				float xx = Mathf.Clamp ((v.x - bmin.x) / (bmax.x - bmin.x) * 65535.0f, 0.0f, 65535.0f);
				float yy = Mathf.Clamp ((v.y - bmin.y) / (bmax.y - bmin.y) * 65535.0f, 0.0f, 65535.0f);
				float zz = Mathf.Clamp ((v.z - bmin.z) / (bmax.z - bmin.z) * 65535.0f, 0.0f, 65535.0f);
				System.UInt16 ix = (System.UInt16)xx;
				System.UInt16 iy = (System.UInt16)yy;
				System.UInt16 iz = (System.UInt16)zz;
				buf.Write (ix);
				buf.Write (iy);
				buf.Write (iz);
			}
		}


	    static void WriteColor32Array(Color32[] arr, BinaryWriter buf)
	    {
	        foreach (Color32 c in arr)
	        {
	            buf.Write(c.r);
	            buf.Write(c.g);
	            buf.Write(c.b);
	            buf.Write(c.a);
	        }
	    }

	    static void ReadColor32Array(Color32[] arr, BinaryReader buf)
	    {
	        if (arr.Length == 0)
	            return;

	        var n = arr.Length;

	        byte[] bytes = buf.ReadBytes(n * 4);

	        int stride = 0;
	        for (int i = 0; i < n; ++i)
	        {
	            byte r = bytes[stride];
	            stride++;

	            byte g = bytes[stride];
	            stride++;

	            byte b = bytes[stride];
	            stride++;

	            byte a = bytes[stride];
	            stride++;

	            arr[i] = new Color32(r, g, b, a);
	        }
	    }

	    static void ReadVector2Array16bit (Vector2[] arr, BinaryReader buf)
		{
			var n = arr.Length;
			if (n == 0)
				return;
			
			// Read bounding box
			Vector2 bmin;
			Vector2 bmax;
			bmin.x = buf.ReadSingle ();
			bmax.x = buf.ReadSingle ();
			bmin.y = buf.ReadSingle ();
			bmax.y = buf.ReadSingle ();

	        byte[] bytes = buf.ReadBytes(n * 2 * 2);
	        byte[] shortBuffer = new byte[2];

	        // Decode vectors as 16 bit integer components between the bounds
	        int stride = 0;
	        for (var i = 0; i < n; ++i)
	        {
	            shortBuffer[0] = bytes[stride];
	            stride++;
	            shortBuffer[1] = bytes[stride];
	            stride++;
	            System.UInt16 ix = (ushort)(shortBuffer[0] | (shortBuffer[1] << 8));

	            shortBuffer[0] = bytes[stride];
	            stride++;
	            shortBuffer[1] = bytes[stride];
	            stride++;
	            System.UInt16 iy = (ushort)(shortBuffer[0] | (shortBuffer[1] << 8));

	            float xx = ix / 65535.0f * (bmax.x - bmin.x) + bmin.x;
				float yy = iy / 65535.0f * (bmax.y - bmin.y) + bmin.y;
				arr[i] = new Vector2 (xx,yy);
			}
		}
		
		static void WriteVector2Array16bit (Vector2[] arr, BinaryWriter buf)
		{
			if (arr.Length == 0)
				return;
			
			// Calculate bounding box of the array
			Vector2 bmin = arr[0] - new Vector2(0.001f, 0.001f);
			Vector2 bmax = arr[0] + new Vector2(0.001f, 0.001f);
			foreach (Vector2 v in arr) {
				bmin.x = Mathf.Min (bmin.x, v.x);
				bmin.y = Mathf.Min (bmin.y, v.y);
				bmax.x = Mathf.Max (bmax.x, v.x);
				bmax.y = Mathf.Max (bmax.y, v.y);
			}
			
			// Write bounds to stream
			buf.Write (bmin.x);
			buf.Write (bmax.x);
			buf.Write (bmin.y);
			buf.Write (bmax.y);
			
			// Encode vectors as 16 bit integer components between the bounds
			foreach (Vector2 v in arr) {
				float xx = (v.x - bmin.x) / (bmax.x - bmin.x) * 65535.0f;
				float yy = (v.y - bmin.y) / (bmax.y - bmin.y) * 65535.0f;
				System.UInt16 ix  = (System.UInt16)xx;
				System.UInt16 iy = (System.UInt16)yy;
				buf.Write (ix);
				buf.Write (iy);
			}
		}
		
		static void ReadVector3ArrayBytes (Vector3[] arr, BinaryReader buf)
		{
			// Decode vectors as 8 bit integers components in -1.0 .. 1.0 range
			var n = arr.Length;

	        byte[] bytes = buf.ReadBytes(n * 3);

	        // Decode vectors as 16 bit integer components between the bounds
	        int stride = 0;
	        for (int i = 0; i < n; ++i)
	        {
				float xx  = (bytes[stride] - 128.0f) / 127.0f;
	            stride++;

	            float yy = (bytes[stride] - 128.0f) / 127.0f;
	            stride++;

	            float zz = (bytes[stride] - 128.0f) / 127.0f;
	            stride++;

	            arr[i] = new Vector3(xx,yy,zz);
			}
		}
		
		static void WriteVector3ArrayBytes (Vector3[] arr, BinaryWriter buf)
		{
			// Encode vectors as 8 bit integers components in -1.0 .. 1.0 range
			foreach (Vector3 v in arr) 
			{
				byte ix = (byte)Mathf.Clamp (v.x * 127.0f + 128.0f, 0.0f, 255.0f);
				byte iy = (byte)Mathf.Clamp (v.y * 127.0f + 128.0f, 0.0f, 255.0f);
				byte iz = (byte)Mathf.Clamp (v.z * 127.0f + 128.0f, 0.0f, 255.0f);
				buf.Write (ix);
				buf.Write (iy);
				buf.Write (iz);
			}
		}

	    static void WriteNormalizedVector3Bytes(Vector3[] arr, BinaryWriter buf)
	    {
	        // Encode vectors as 8 bit integers components in -1.0 .. 1.0 range
	        for (int i = 0; i < arr.Length; i++)
	        {
	            Vector3 v = arr[i];

	            byte ix = (byte)Mathf.Clamp(v.x * 127.0f + 128.0f, 0.0f, 255.0f);
	            byte iy = (byte)Mathf.Clamp(v.y * 127.0f + 128.0f, 0.0f, 255.0f);
	            byte iz = (byte)Mathf.Clamp(v.z * 127.0f + 128.0f, 0.0f, 255.0f);
	            buf.Write(ix);
	            buf.Write(iy);
	            buf.Write(iz);
	        }
	    }

	    static void ReadVector4ArrayBytes (Vector4[] arr, BinaryReader buf)
		{
	        var n = arr.Length;

	        byte[] bytes = buf.ReadBytes(n * 4);

	        // Decode vectors as 8 bit integers components in -1.0 .. 1.0 range
	        int stride = 0;
	        for (int i = 0; i < n; ++i)
	        {
	            float xx = (bytes[stride] - 128.0f) / 127.0f;
	            stride++;

	            float yy = (bytes[stride] - 128.0f) / 127.0f;
	            stride++;

	            float zz = (bytes[stride] - 128.0f) / 127.0f;
	            stride++;

	            float ww = (bytes[stride] - 128.0f) / 127.0f;
	            stride++;

	            arr[i] = new Vector4(xx,yy,zz,ww);
			}
		}
		
		static void WriteVector4ArrayBytes (Vector4[] arr, BinaryWriter buf)
		{
			// Encode vectors as 8 bit integers components in -1.0 .. 1.0 range
			foreach (Vector4 v in arr) {
				byte ix = (byte)Mathf.Clamp (v.x * 127.0f + 128.0f, 0.0f, 255.0f);
				byte iy = (byte)Mathf.Clamp (v.y * 127.0f + 128.0f, 0.0f, 255.0f);
				byte iz = (byte)Mathf.Clamp (v.z * 127.0f + 128.0f, 0.0f, 255.0f);
				byte iw = (byte)Mathf.Clamp (v.w * 127.0f + 128.0f, 0.0f, 255.0f);
				buf.Write (ix);
				buf.Write (iy);
				buf.Write (iz);
				buf.Write (iw);
			}
		}
		
		static void WriteMatrix4x4 (Matrix4x4[] arr, BinaryWriter buf)
		{
			if (arr.Length == 0)
				return;
			
			foreach (Matrix4x4 matrix in arr)
			{
				//first column
				float m00 = matrix.m00;
				float m01 = matrix.m01;
				float m02 = matrix.m02;
				float m03 = matrix.m03;

				//second column
				float m10 = matrix.m10;
				float m11 = matrix.m11;
				float m12 = matrix.m12;
				float m13 = matrix.m13;

				//third column
				float m20 = matrix.m20;
				float m21 = matrix.m21;
				float m22 = matrix.m22;
				float m23 = matrix.m23;

				//fourth column
				float m30 = matrix.m30;
				float m31 = matrix.m31;
				float m32 = matrix.m32;
				float m33 = matrix.m33;

				buf.Write (m00);
				buf.Write (m01);
				buf.Write (m02);
				buf.Write (m03);

				buf.Write (m10);
				buf.Write (m11);
				buf.Write (m12);
				buf.Write (m13);

				buf.Write (m20);
				buf.Write (m21);
				buf.Write (m22);
				buf.Write (m23);

				buf.Write (m30);
				buf.Write (m31);
				buf.Write (m32);
				buf.Write (m33);
			}
		}

		static void ReadMatrix4x4Array (Matrix4x4 [] arr , BinaryReader buf )
		{
			if (arr.Length == 0)
				return;
			
			int n = arr.Length;
			for (int i = 0; i < n; ++i)
			{
				//first column
				float m00 = buf.ReadSingle();
				float m01 = buf.ReadSingle();
				float m02 = buf.ReadSingle();
				float m03 = buf.ReadSingle();
				
				//second column
				float m10 = buf.ReadSingle();
				float m11 = buf.ReadSingle();
				float m12 = buf.ReadSingle();
				float m13 = buf.ReadSingle();
				
				//third column
				float m20 = buf.ReadSingle();
				float m21 = buf.ReadSingle();
				float m22 = buf.ReadSingle();
				float m23 = buf.ReadSingle();
				
				//fourth column
				float m30 = buf.ReadSingle();
				float m31 = buf.ReadSingle();
				float m32 = buf.ReadSingle();
				float m33 = buf.ReadSingle();

				arr[i].m00 = m00;
				arr[i].m01 = m01;
				arr[i].m02 = m02;
				arr[i].m03 = m03;

				arr[i].m10 = m10;
				arr[i].m11 = m11;
				arr[i].m12 = m12;
				arr[i].m13 = m13;

				arr[i].m20 = m20;
				arr[i].m21 = m21;
				arr[i].m22 = m22;
				arr[i].m23 = m23;

				arr[i].m30 = m30;
				arr[i].m31 = m31;
				arr[i].m32 = m32;
				arr[i].m33 = m33;
			}
		}
	    static void ReadBoneWeightArrayBytes (BoneWeight [] arr , BinaryReader buf )
		{
			if (arr.Length == 0)
				return;

	        //Size of a single element
	        //(4 + 4 + 4 + 4) + (2 + 2 + 2 + 2)
	        int elementSize = 24;
	        byte[] elementBuffer = new byte[elementSize];
			int n = arr.Length;

			for (int i = 0; i < n; ++i)
			{
	            buf.Read(elementBuffer, 0, elementBuffer.Length);
	            int stride = 0;

	            float weight0 = BitConverter.ToSingle(elementBuffer, stride);
	            stride += 4;
	            float weight1 = BitConverter.ToSingle(elementBuffer, stride);
	            stride += 4;
	            float weight2 = BitConverter.ToSingle(elementBuffer, stride);
	            stride += 4;
	            float weight3 = BitConverter.ToSingle(elementBuffer, stride);
	            stride += 4;

	            System.UInt16 boneIndex0 = (ushort)(elementBuffer[0+stride] | (elementBuffer[1+stride] << 8));
	            stride += 2;

	            System.UInt16 boneIndex1 = (ushort)(elementBuffer[0 + stride] | (elementBuffer[1 + stride] << 8));
	            stride += 2;

	            System.UInt16 boneIndex2 = (ushort)(elementBuffer[0 + stride] | (elementBuffer[1 + stride] << 8));
	            stride += 2;

	            System.UInt16 boneIndex3 = (ushort)(elementBuffer[0 + stride] | (elementBuffer[1 + stride] << 8));
	            stride += 2;

	            arr[i].weight0 = weight0;
	            arr[i].weight1 = weight1;
	            arr[i].weight2 = weight2;
	            arr[i].weight3 = weight3;
	            arr[i].boneIndex0 = (int)boneIndex0;
	            arr[i].boneIndex1 = (int)boneIndex1;
	            arr[i].boneIndex2 = (int)boneIndex2;
	            arr[i].boneIndex3 = (int)boneIndex3;
	        }
		}
		
		static void WriteBoneWeightArrayBytes (BoneWeight [] arr , BinaryWriter buf )
		{
			if (arr.Length == 0)
				return;

			foreach (BoneWeight bone in arr)
			{
				float weight0 = bone.weight0;
				float weight1 = bone.weight1;
				float weight2 = bone.weight2;
				float weight3 = bone.weight3;
				System.UInt16 boneIndex0 = (System.UInt16)bone.boneIndex0;
				System.UInt16 boneIndex1 = (System.UInt16)bone.boneIndex1;
				System.UInt16 boneIndex2 = (System.UInt16)bone.boneIndex2;
				System.UInt16 boneIndex3 = (System.UInt16)bone.boneIndex3;
				
				buf.Write (weight0);
				buf.Write (weight1);
				buf.Write (weight2);
				buf.Write (weight3);
				buf.Write (boneIndex0);
				buf.Write (boneIndex1);
				buf.Write (boneIndex2);
				buf.Write (boneIndex3);
			}
		}
		
		// Writes mesh to an array of bytes.
		public static byte[] WriteMesh(Mesh mesh, bool saveTangents, bool writeSkinning)
		{
			if( !mesh )
			{
				Debug.Log( "No mesh given!" );
				return null;
			}
			
			Vector3[] verts = mesh.vertices;
			Vector3[] normals = mesh.normals;
			Vector4[] tangents = mesh.tangents;
			Vector2[] uvs = mesh.uv;
			int[] tris = mesh.triangles;
			Matrix4x4[] bindPoses = mesh.bindposes;
			BoneWeight[] boneWeights = mesh.boneWeights;
			
			// figure out vertex format
			byte format = 1;
			if( normals.Length > 0 )
				format |= 2;
			if( saveTangents && tangents.Length > 0 )
				format |= 4;
			if( uvs.Length > 0 )
				format |= 8;
			
			MemoryStream stream = new MemoryStream();
			BinaryWriter buf = new BinaryWriter( stream );
			
			// write header
			System.UInt16 vertCount = (System.UInt16)verts.Length;
			System.UInt16 triCount = (System.UInt16)(tris.Length / 3);
			System.UInt16 bindPoseCount = (System.UInt16)bindPoses.Length;

			buf.Write( vertCount );
			buf.Write( triCount );

			if (writeSkinning)
				buf.Write( bindPoseCount);

			buf.Write( format );

			// vertex components
			WriteVector3Array16bit (verts, buf);
			WriteVector3ArrayBytes (normals, buf);
			if (saveTangents)
				WriteVector4ArrayBytes (tangents, buf);
			WriteVector2Array16bit (uvs, buf);

			if (writeSkinning)
			{
				WriteMatrix4x4(bindPoses, buf);
				WriteBoneWeightArrayBytes(boneWeights, buf);
			}

			// triangle indices
			foreach( int idx in tris ) 
			{
				System.UInt16 idx16 = (System.UInt16)idx;
				buf.Write( idx16 );
			}

	        //Check if mesh has blendshapes, serialize them
	        System.UInt16 blendShapeCount = (System.UInt16)mesh.blendShapeCount;
	        buf.Write(blendShapeCount);

	        if (blendShapeCount > 0)
	        {
	            Vector3[] deltaVertices = new Vector3[vertCount];
	            Vector3[] deltaNormals = new Vector3[vertCount];
	            Vector3[] deltaTangents = new Vector3[vertCount];

	            for (int i = 0; i < mesh.blendShapeCount; i++)
	            {
	                byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(mesh.GetBlendShapeName(i));

	                System.UInt16 nameLength = (System.UInt16)nameBytes.Length;

	                buf.Write(nameLength);
	                buf.Write(nameBytes);

	                mesh.GetBlendShapeFrameVertices(i, 0, deltaVertices, deltaNormals, deltaTangents);

	                WriteVector3Array16bit(deltaVertices, buf);
	                WriteNormalizedVector3Bytes(deltaNormals, buf);
	                WriteNormalizedVector3Bytes(deltaTangents, buf);
	            }
	        }

	        Vector3[] bounds = new Vector3[2];
	        bounds[0] = mesh.bounds.center;
	        bounds[1] = mesh.bounds.size;

	        WriteVector3Array16bit(bounds, buf);

	        Color32[] vertColor = mesh.colors32;
	        if (vertColor.Length > 0)
	        {
	            System.UInt16 vertColorCount = (System.UInt16)vertColor.Length;

	            buf.Write(vertColorCount);

	            WriteColor32Array(vertColor, buf);
	        }

	        buf.Close();
			
			return stream.ToArray();
		}

		public static Mesh ReadMesh(byte[] bytes, bool readSkinning)
		{
			if( bytes == null || bytes.Length < 5 )
			{
				Debug.Log( "Invalid mesh file!" );
				return null;
			}
			
			BinaryReader buf = new BinaryReader( new MemoryStream( bytes ) );
			
			// read header
			System.UInt16 vertCount = buf.ReadUInt16();
			System.UInt16 triCount = buf.ReadUInt16();

			System.UInt16 bindPoseCount = 0;
			if (readSkinning)
				bindPoseCount = buf.ReadUInt16();

			var format = buf.ReadByte();
			
			// sanity check
			if (vertCount < 0 || vertCount > 64000)
			{
				Debug.Log ("Invalid vertex count in the mesh data!");
				return null;
			}
			if (triCount < 0 || triCount > 64000)
			{
				Debug.Log ("Invalid triangle count in the mesh data!");
				return null;
			}
			if (format < 1 || (format&1) == 0 || format > 15)
			{
				Debug.Log ("Invalid vertex format in the mesh data!");
				return null;
			}
			
			Mesh mesh = new Mesh();
			int i = 0;
			
			// positions
			Vector3[] verts = new Vector3[vertCount];
			ReadVector3Array16bit (verts, buf);
			mesh.vertices = verts;

			Vector3[] normals = new Vector3[vertCount];
			ReadVector3ArrayBytes (normals, buf);
			mesh.normals = normals;

			Vector4[] tangents = new Vector4[vertCount];
			ReadVector4ArrayBytes (tangents, buf);
			mesh.tangents = tangents;

			Vector2[] uvs = new Vector2[vertCount];
			ReadVector2Array16bit (uvs, buf);
			mesh.uv = uvs;

			if (readSkinning)
			{
				Matrix4x4[] bindPoses = new Matrix4x4[bindPoseCount];
				ReadMatrix4x4Array (bindPoses, buf);
				mesh.bindposes = bindPoses;

				BoneWeight[] boneWeights = new BoneWeight[vertCount];
				ReadBoneWeightArrayBytes (boneWeights, buf);
				mesh.boneWeights = boneWeights;
			}

			// triangle indices
			var tris = new int[triCount * 3];
	        byte[] triBuffer = buf.ReadBytes(tris.Length * 2);
	        byte[] shortBuffer = new byte[2];
	        int stride = 0;
	        for (i = 0; i < tris.Length; ++i)
	        {
	            shortBuffer[0] = triBuffer[stride];
	            stride++;

	            shortBuffer[1] = triBuffer[stride];
	            stride++;

	            tris[i] = BitConverter.ToUInt16(shortBuffer, 0);
	        }
	        mesh.triangles = tris;

	        //TODO: Check what happens with older meshes, could crash app...

	        // Check if Mesh has blendshape
	        int blendShapeCount = (int)buf.ReadUInt16();

	        if (blendShapeCount > 0)
	        {
	            Vector3[] deltaVertices = new Vector3[vertCount];
	            Vector3[] deltaNormals = new Vector3[vertCount];
	            Vector3[] deltaTangents = new Vector3[vertCount];

	            for (int j = 0; j < blendShapeCount; j++)
	            {
	                System.UInt16 nameLength = buf.ReadUInt16();
	                byte[] name = buf.ReadBytes((int)nameLength);
	                string blendShapeName = System.Text.Encoding.UTF8.GetString(name);

	                ReadVector3Array16bit(deltaVertices, buf);
	                ReadVector3ArrayBytes(deltaNormals, buf);
	                ReadVector3ArrayBytes(deltaTangents, buf);

	                mesh.AddBlendShapeFrame(blendShapeName, 100, deltaVertices, deltaNormals, deltaTangents);
	            }
	        }

	        Vector3[] boundsVectors = new Vector3[2];
	        ReadVector3Array16bit(boundsVectors, buf);

	        mesh.bounds = new Bounds(boundsVectors[0], boundsVectors[1]);

	        //Make sure we have not reached end of stream before attempting this.
	        if (buf.BaseStream.Position != buf.BaseStream.Length)
	        {
	            int vertexColorCount = (int)buf.ReadUInt16();

	            if (vertexColorCount > 0)
	            {
	                Color32[] vertColors = new Color32[vertexColorCount];

	                ReadColor32Array(vertColors, buf);

	                mesh.colors32 = vertColors;
	            }
	        }

	        buf.Close();
			
			return mesh;
		}
		
		// <Summary>
		// Description: Setting the Indexes for the new bones	
		// <Summary>
		public static BoneWeight RecalculateBoneIndexes(BoneWeight bw, Dictionary<string, int> boneMapping, string[] boneNames)
		{
			BoneWeight retBw = bw;
			retBw.boneIndex0 = boneMapping[boneNames[bw.boneIndex0]];
			retBw.boneIndex1 = boneMapping[boneNames[bw.boneIndex1]];
			retBw.boneIndex2 = boneMapping[boneNames[bw.boneIndex2]];
			retBw.boneIndex3 = boneMapping[boneNames[bw.boneIndex3]];
			return retBw;
		}
	}
}