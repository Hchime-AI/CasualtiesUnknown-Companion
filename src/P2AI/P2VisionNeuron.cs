using System;
using UnityEngine;

namespace CUCompanion.P2AI
{
	/// <summary>
	/// 视觉神经元：仅提供游戏画面与视场快照能力。
	/// </summary>
	public class P2VisionNeuron
	{
		private Texture2D lastCapture;
		private float lastCaptureTime;

		public Texture2D LastCapture => lastCapture;
		public float LastCaptureTime => lastCaptureTime;

		/// <summary>
		/// 截取当前画面为 Texture2D
		/// </summary>
		public Texture2D CaptureViewport()
		{
			try
			{
				int width = Screen.width;
				int height = Screen.height;
				if (width <= 0 || height <= 0) return null;

				if (lastCapture != null)
				{
					UnityEngine.Object.Destroy(lastCapture);
					lastCapture = null;
				}

				lastCapture = new Texture2D(width, height, TextureFormat.RGB24, false);
				lastCapture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				lastCapture.Apply();
				lastCaptureTime = Time.unscaledTime;
				return lastCapture;
			}
			catch (Exception ex)
			{
				CompanionPlugin.Log?.LogWarning($"[P2VisionNeuron] Screen capture error: {ex.Message}");
				return null;
			}
		}

		/// <summary>
		/// 截取并转为 PNG 字节数组
		/// </summary>
		public byte[] CapturePngBytes()
		{
			Texture2D tex = CaptureViewport();
			if (tex == null) return null;
			try
			{
				return ImageConversion.EncodeToPNG(tex);
			}
			catch (Exception ex)
			{
				CompanionPlugin.Log?.LogWarning($"[P2VisionNeuron] Encode PNG error: {ex.Message}");
				return null;
			}
		}
	}
}
