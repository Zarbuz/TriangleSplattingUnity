using System.Globalization;
using System.IO;
using UnityEngine;

namespace TriangleSplattingUnity.Runtime
{
	public class TriangleSplatOctreeRenderer : MonoBehaviour
	{
		public string coffFilePath;
		public Shader shader;
		public ComputeShader cullingCompute;
		public uint maxTriangleInstances = 500_000;

		private ComputeBuffer v0Buffer, v1Buffer, v2Buffer, colorBuffer;
		private ComputeBuffer visibleIndexBuffer, argsBuffer;
		private Material material;
		private Mesh triangleMesh;

		private Camera cam;
		private int kernel;

		private void Start()
		{
			cam = Camera.main;
			material = new Material(shader);
			kernel = cullingCompute.FindKernel("CSMain");
			LoadAndInit();
		}

		void LoadAndInit()
		{
			string[] lines = File.ReadAllLines(coffFilePath);
			if (!lines[0].StartsWith("COFF")) return;

			int vertexCount = int.Parse(lines[1].Split()[0]);
			int faceCount = int.Parse(lines[1].Split()[1]);

			Vector3[] verts = new Vector3[vertexCount];
			for (int i = 0; i < vertexCount; i++)
			{
				var p = lines[2 + i].Split();
				verts[i] = new Vector3(
					float.Parse(p[0], CultureInfo.InvariantCulture),
					float.Parse(p[1], CultureInfo.InvariantCulture),
					float.Parse(p[2], CultureInfo.InvariantCulture)
				);
			}

			var v0s = new Vector3[faceCount];
			var v1s = new Vector3[faceCount];
			var v2s = new Vector3[faceCount];
			var colors = new Color[faceCount];

			int count = 0;
			for (int i = 0; i < faceCount; i++)
			{
				var p = lines[2 + vertexCount + i].Split();
				if (p[0] != "3") continue;

				int i0 = int.Parse(p[1]);
				int i1 = int.Parse(p[2]);
				int i2 = int.Parse(p[3]);

				v0s[count] = verts[i0];
				v1s[count] = verts[i1];
				v2s[count] = verts[i2];

				colors[count] = new Color(
					float.Parse(p[4]) / 255f,
					float.Parse(p[5]) / 255f,
					float.Parse(p[6]) / 255f,
					float.Parse(p[7]) / 255f
				);

				count++;
			}

			v0Buffer = new ComputeBuffer(count, sizeof(float) * 3);
			v1Buffer = new ComputeBuffer(count, sizeof(float) * 3);
			v2Buffer = new ComputeBuffer(count, sizeof(float) * 3);
			colorBuffer = new ComputeBuffer(count, sizeof(float) * 4);
			visibleIndexBuffer = new ComputeBuffer((int)maxTriangleInstances, sizeof(uint), ComputeBufferType.Append);
			argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

			v0Buffer.SetData(v0s);
			v1Buffer.SetData(v1s);
			v2Buffer.SetData(v2s);
			colorBuffer.SetData(colors);
			argsBuffer.SetData(new uint[] { 3, maxTriangleInstances, 0, 0, 0 });

			material.SetBuffer("_Vertices0", v0Buffer);
			material.SetBuffer("_Vertices1", v1Buffer);
			material.SetBuffer("_Vertices2", v2Buffer);
			material.SetBuffer("_Colors", colorBuffer);
			material.SetBuffer("_VisibleIndices", visibleIndexBuffer);
		}

		void Update()
		{
			if (v0Buffer == null) return;

			Matrix4x4 VP = cam.projectionMatrix * cam.worldToCameraMatrix;
			visibleIndexBuffer.SetCounterValue(0);

			cullingCompute.SetMatrix("_VPMatrix", VP);
			cullingCompute.SetBuffer(kernel, "_V0", v0Buffer);
			cullingCompute.SetBuffer(kernel, "_V1", v1Buffer);
			cullingCompute.SetBuffer(kernel, "_V2", v2Buffer);
			cullingCompute.SetBuffer(kernel, "_VisibleIndices", visibleIndexBuffer);
			cullingCompute.Dispatch(kernel, Mathf.CeilToInt(v0Buffer.count / 64f), 1, 1);

			Graphics.DrawMeshInstancedIndirect(
				triangleMesh ??= CreateTriangleMesh(),
				0,
				material,
				new Bounds(Vector3.zero, Vector3.one * 10000f),
				argsBuffer
			);
		}

		Mesh CreateTriangleMesh()
		{
			var mesh = new Mesh();
			mesh.vertices = new[]
			{
				Vector3.zero,
				Vector3.right,
				Vector3.up
			};
			mesh.triangles = new[] { 0, 1, 2 };
			return mesh;
		}

		private void OnDestroy()
		{
			v0Buffer?.Dispose();
			v1Buffer?.Dispose();
			v2Buffer?.Dispose();
			colorBuffer?.Dispose();
			visibleIndexBuffer?.Dispose();
			argsBuffer?.Dispose();
		}
	}
}