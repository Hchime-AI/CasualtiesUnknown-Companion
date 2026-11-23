using System;

namespace CUCompanion.P2AI
{
	/// <summary>
	/// 听觉神经元（暂不做处理，预留接口）
	/// </summary>
	public class P2AudioNeuron
	{
		public bool Active { get; set; } = false;
	}

	/// <summary>
	/// 破坏神经元（暂不做处理，预留接口）
	/// </summary>
	public class P2BreakNeuron
	{
		public bool Active { get; set; } = false;
	}

	/// <summary>
	/// 迷乱神经元（暂不做处理，预留接口）
	/// </summary>
	public class P2ChaosNeuron
	{
		public bool Active { get; set; } = false;
	}

	/// <summary>
	/// 娱乐神经元（暂不做处理，预留接口）
	/// </summary>
	public class P2PlayNeuron
	{
		public bool Active { get; set; } = false;
	}
}
