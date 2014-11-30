using System;
using System.IO;

namespace BSPVirtualLoader
{
	public class VirtualLoader
	{

		public BSP.BSPLoader bsploader {
			get;
			private set;
		}
		public VirtualLoader (String filename)
		{
			FileStream fileStream = File.OpenRead(filename);
			BinaryReader br = new BinaryReader(fileStream);
			bsploader = new BSP.BSPLoader ();
			bsploader.LoadBSP (br);
		}
	}
}

