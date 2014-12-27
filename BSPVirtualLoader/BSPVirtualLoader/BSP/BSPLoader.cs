using System;
using System.Runtime.InteropServices;
using System.IO;

namespace BSPVirtualLoader.BSP
{
	public class BSPLoader
	{
		public const int HEADER_LUMPS = 64;
		public const int MAX_MAP_PLANES = 65536;
		public const int MAX_MAP_VERTS = 65536;
		public const int MAX_MAP_EDGES = 256000;
		public const int MAX_MAP_SURFEDGES = 512000;
		public const int MAX_MAP_BRUSHES = 8192;
		public const int MAX_MAP_NODES = 65536;
		public const int MAX_MAP_LEAFFACES = 65536;
		public const int MAX_MAP_LEAFBRUSHES = 65536;

		//16 bytes
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct lump_t {
			public int fileofs;				// offset into file (bytes)
			public int filelen;				// length of lump (bytes)
			public int version;				// lump format version
			public char[] fourCC;				// lump ident code, size 4
			public lump_t(BinaryReader br) {
				fileofs=br.ReadInt32();
				filelen=br.ReadInt32();
				version=br.ReadInt32();
				fourCC=new char[4];
				Array.Copy(br.ReadChars(4),fourCC,4);//Be carefull that char must be 1 byte only
			}
		}

		//1036 bytes
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dheader_t {
			public int ident;		// BSP file identifier, little-endian "VBSP"   0x50534256
			public int version;		// BSP file version, 21 for CS:GO
			public lump_t[] lumps;	//Size should be HEADER_LUMPS
			public int mapRevision;	// the map's revision (iteration, version) number

			public dheader_t (BinaryReader br) {
				ident=br.ReadInt32();
				version=br.ReadInt32();
				lumps = new lump_t[HEADER_LUMPS];
				for(int i=0 ; i<HEADER_LUMPS ; i++) {
					lumps[i]=new lump_t(br);
				}
				mapRevision=br.ReadInt32();
			}
		}

		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct Vector {
			public float x,y,z;
			public Vector(BinaryReader br) {
				x=br.ReadSingle();
				y=br.ReadSingle();
				z=br.ReadSingle();
			}
		}

		//Lump 1, 20 bytes
		public static int STRUCT_PLANE_SIZE = 20;
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dplane_t {
			public Vector normal;	// normal vector
			public float dist;		// distance from origin
			public int type;		// plane axis identifier
			public dplane_t(BinaryReader br) {
				normal=new Vector(br);
				dist=br.ReadSingle();
				type=br.ReadInt32();
			}
		}

		//Lump 3 => array of vertices => Vector (x,y,z)

		//Lump 12
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dedge_t {
			public ushort[] v;	// vertex indices, size 2
			public dedge_t(BinaryReader br) {
				v=new ushort[2];
				v[0]=br.ReadUInt16();
				v[1]=br.ReadUInt16();
			}
		}

		//Lump 13 => surfedge => array of signed int

		//Lump 7, 56 bytes
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dface_t {
			public ushort	planenum;		// the plane number
			public byte		side;			// faces opposite to the node's plane direction
			public byte		onNode;			// 1 of on node, 0 if in leaf
			public int		firstedge;		// index into surfedges
			public short		numedges;		// number of surfedges
			public short		texinfo;		// texture info
			public short		dispinfo;		// displacement info
			public short		surfaceFogVolumeID;	// ?
			public byte[]		style;		// switchable lighting info, size 4
			public int		lightofs;		// offset into lightmap lump
			public float		area;			// face area in units^2
			public int[]		LightmapTextureMinsInLuxels;	// texture lighting info, size 2
			public int[]		LightmapTextureSizeInLuxels;	// texture lighting info, size 2
			public int		origFace;		// original face this was split from
			public ushort	numPrims;		// primitives
			public ushort	firstPrimID;
			public uint	smoothingGroups;	// lightmap smoothing group
			public dface_t(BinaryReader br) {
				planenum=br.ReadUInt16();
				side=br.ReadByte();
				onNode=br.ReadByte();
				firstedge=br.ReadInt32();
				numedges=br.ReadInt16();
				texinfo=br.ReadInt16();
				dispinfo=br.ReadInt16();
				surfaceFogVolumeID=br.ReadInt16();
				style=new byte[4];
				Array.Copy(br.ReadBytes(4),style,4);
				lightofs=br.ReadInt32();
				area=br.ReadSingle();
				LightmapTextureMinsInLuxels=new int[2];
				LightmapTextureMinsInLuxels[0]=br.ReadInt32();
				LightmapTextureMinsInLuxels[1]=br.ReadInt32();
				LightmapTextureSizeInLuxels=new int[2];
				LightmapTextureSizeInLuxels[0]=br.ReadInt32();
				LightmapTextureSizeInLuxels[1]=br.ReadInt32();
				origFace=br.ReadInt32();
				numPrims=br.ReadUInt16();
				firstPrimID=br.ReadUInt16();
				smoothingGroups=br.ReadUInt32();
			}
		}

		//Lump 18
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dbrush_t {
			public int	firstside;	// first brushside
			public int	numsides;	// number of brushsides
			public int	contents;	// contents flags, see contentTypes
			public dbrush_t(BinaryReader br) {
				firstside=br.ReadInt32();
				numsides=br.ReadInt32();
				contents=br.ReadInt32();
			}
		}

		//Lump 19
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dbrushside_t {
			public ushort		planenum;	// facing out of the leaf
			public short		texinfo;	// texture info
			public short		dispinfo;	// displacement info
			public short		bevel;		// is the side a bevel plane?
			public dbrushside_t(BinaryReader br) {
				planenum=br.ReadUInt16();
				texinfo=br.ReadInt16();
				dispinfo=br.ReadInt16();
				bevel=br.ReadInt16();
			}
		}

		//Lump 5
		public static int STRUCT_BSPNODE_SIZE = 32;
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dnode_t {
			public int		planenum;	// index into plane array
			public int[]	children;	// negative numbers are -(leafs + 1), not nodes
			public short[]	mins;		// for frustum culling
			public short[]	maxs;
			public ushort	firstface;	// index into face array
			public ushort	numfaces;	// counting both sides
			public short	area;		// If all leaves below this node are in the same area, then
												// this is the area index. If not, this is -1.
			public short	padding;	// pad to 32 bytes length
			public dnode_t(BinaryReader br) {
				planenum=br.ReadInt32();
				children=new int[2];
				children[0]=br.ReadInt32();
				children[1]=br.ReadInt32();
				mins=new short[3];
				mins[0]=br.ReadInt16();
				mins[1]=br.ReadInt16();
				mins[2]=br.ReadInt16();
				maxs=new short[3];
				maxs[0]=br.ReadInt16();
				maxs[1]=br.ReadInt16();
				maxs[2]=br.ReadInt16();
				firstface=br.ReadUInt16();
				numfaces=br.ReadUInt16();
				area=br.ReadInt16();
				padding=br.ReadInt16();//padding, but let's load it anyway
			}
		}

		//Lump 10
		public static int STRUCT_BSPLEAF_SIZE = 32;
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dleaf_t {
			public int			contents;		// OR of all brushes (not needed?)
			public short		cluster;		// cluster this leaf is in
			public short		area;			// area this leaf is in
			public short		flags;		// flags
			public short[]		mins;		// for frustum culling
			public short[]		maxs;
			public ushort		firstleafface;	// index into leaffaces
			public ushort		numleaffaces;
			public ushort		firstleafbrush;	// index into leafbrushes
			public ushort		numleafbrushes;
			public short		leafWaterDataID;// -1 for not in water

			//!!! NOTE: for maps of version 19 or lower uncomment this block
			/*
			CompressedLightCube	ambientLighting;	// Precaculated light info for entities.
			short			padding;		// padding to 4-byte boundary
			*/
			public dleaf_t(BinaryReader br) {
				contents=br.ReadInt32();
				cluster=br.ReadInt16();
				area=br.ReadInt16();
				flags=br.ReadInt16();
				mins=new short[3];
				mins[0]=br.ReadInt16();
				mins[1]=br.ReadInt16();
				mins[2]=br.ReadInt16();
				maxs=new short[3];
				maxs[0]=br.ReadInt16();
				maxs[1]=br.ReadInt16();
				maxs[2]=br.ReadInt16();
				firstleafface=br.ReadUInt16();
				numleaffaces=br.ReadUInt16();
				firstleafbrush=br.ReadUInt16();
				numleafbrushes=br.ReadUInt16();
				leafWaterDataID=br.ReadInt16();
			}
		}

		//Lump 16: leafface, array of ushort
		//Lump 17: leafbrushes, array of ushort

		//Lump 14
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dmodel_t{
			public Vector	mins;		// bounding box
			public Vector	maxs;		// bounding box
			public Vector	origin;		// for sounds or lights
			public int		headnode;		// index into node array
			public int		firstface;		// index into face array
			public int		numfaces;		// index into face array
			public dmodel_t(BinaryReader br) {
				mins=new Vector(br);
				maxs=new Vector(br);
				origin=new Vector(br);
				headnode=br.ReadInt32();
				firstface=br.ReadInt32();
				numfaces=br.ReadInt32();
			}
		}

		//Lump 6
		public static int STRUCT_TEXINFO_SIZE = 72;
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct texinfo_t {
			public float[,]	textureVecs;	// [s/t][xyz offset]
			public float[,]	lightmapVecs;	// [s/t][xyz offset] - length is in units of texels/area
			public int		flags;			// miptex flags	overrides
			public int		texdata;		// Pointer to texture name, size, etc.

			public texinfo_t(BinaryReader br) {
				textureVecs=new float[2, 4];
				for(int i=0 ; i<2 ; i++) {
					for(int j=0 ; j<4 ; j++) {
						textureVecs[i, j]=br.ReadSingle();
					}
				}
				lightmapVecs=new float[2, 4];
				for(int i=0 ; i<2 ; i++) {
					for(int j=0 ; j<4 ; j++) {
						lightmapVecs[i, j]=br.ReadSingle();
					}
				}
				flags=br.ReadInt32();
				texdata=br.ReadInt32();
			}
		}

		//Lump 2
		public static int STRUCT_TEXDATA_SIZE = 32;
		[StructLayout (LayoutKind.Sequential, Pack=1)]
		public struct dtexdata_t {
			public Vector	reflectivity;		// RGB reflectivity
			public int	nameStringTableID;		// index into TexdataStringTable
			public int	width ;					// source image
			public int	height;					// source image
			public int	view_width;
			public int  view_height;
			public dtexdata_t(BinaryReader br) {
				reflectivity=new Vector(br);
				nameStringTableID=br.ReadInt32();
				width=br.ReadInt32();
				height=br.ReadInt32();
				view_width=br.ReadInt32();
				view_height=br.ReadInt32();
			}
		}

		//lump 44, TexdataStringTable array of int, offset into TexdataStringData 

		//Lump 43, TexdataStringData concatenated null-terminated strings

		public enum contentTypes : int {
			CONTENTS_EMPTY = 0,
			CONTENTS_SOLID = 0x1,
			CONTENTS_WINDOW = 0x2,
			CONTENTS_AUX = 0x4,
			CONTENTS_GRATE = 0x8,
			CONTENTS_SLIME = 0x10,
			CONTENTS_WATER = 0x20,
			CONTENTS_MIST = 0x40,
			CONTENTS_OPAQUE  = 0x80,
			CONTENTS_TESTFOGVOLUME = 0x100,
			CONTENTS_UNUSED = 0x200,
			CONTENTS_UNUSED6 = 0x400,
			CONTENTS_TEAM1 = 0x800,
			CONTENTS_TEAM2 = 0x1000,
			CONTENTS_IGNORE_NODRAW_OPAQUE  = 0x2000,
			CONTENTS_MOVEABLE  = 0x4000,
			CONTENTS_AREAPORTAL  = 0x8000,
			CONTENTS_PLAYERCLIP  = 0x10000,
			CONTENTS_MONSTERCLIP  = 0x20000,
			CONTENTS_CURRENT_0  = 0x40000,
			CONTENTS_CURRENT_90  = 0x80000,
			CONTENTS_CURRENT_180  = 0x100000,
			CONTENTS_CURRENT_270  = 0x200000,
			CONTENTS_CURRENT_UP  = 0x400000,
			CONTENTS_CURRENT_DOWN  = 0x800000,
			CONTENTS_ORIGIN  = 0x1000000,
			CONTENTS_MONSTER  = 0x2000000,
			CONTENTS_DEBRIS  = 0x4000000,
			CONTENTS_DETAIL = 0x8000000,
			CONTENTS_TRANSLUCENT  = 0x10000000,
			CONTENTS_LADDER  = 0x20000000,
			CONTENTS_HITBOX  = 0x40000000
		};

		public enum lumpTypes : int {
			LUMP_ENTITIES = 0,
			LUMP_PLANES = 1,
			LUMP_TEXDATA = 2,
			LUMP_VERTEXES = 3,
			LUMP_VISIBILITY = 4,
			LUMP_NODES = 5,
			LUMP_TEXINFO = 6,
			LUMP_FACES = 7,
			LUMP_LIGHTING = 8,
			LUMP_OCCLUSION = 9,
			LUMP_LEAFS = 10,
			LUMP_FACEIDS = 11,
			LUMP_EDGES = 12,
			LUMP_SURFEDGES = 13,
			LUMP_MODELS = 14,
			LUMP_WORLDLIGHTS = 15,
			LUMP_LEAFFACES = 16,
			LUMP_LEAFBRUSHES = 17,
			LUMP_BRUSHES = 18,
			LUMP_BRUSHSIDES = 19,
			LUMP_AREAS = 20,
			LUMP_AREAPORTALS = 21,
			LUMP_UNUSED0 = 22,
			LUMP_UNUSED1 = 23,
			LUMP_UNUSED2 = 24,
			LUMP_UNUSED3 = 25,
			LUMP_DISPINFO = 26,
			LUMP_ORIGINALFACES = 27,
			LUMP_PHYSDISP = 28,
			LUMP_PHYSCOLLIDE = 29,
			LUMP_VERTNORMALS = 30,
			LUMP_VERTNORMALINDICES = 31,
			LUMP_DISP_LIGHTMAP_ALPHAS = 32,
			LUMP_DISP_VERTS = 33,
			LUMP_DISP_LIGHTMAP_SAMPLE_POSITIONS = 34,
			LUMP_GAME_LUMP = 35,
			LUMP_LEAFWATERDATA = 36,
			LUMP_PRIMITIVES = 37,
			LUMP_PRIMVERTS = 38,
			LUMP_PRIMINDICES = 39,
			LUMP_PAKFILE = 40,
			LUMP_CLIPPORTALVERTS = 41,
			LUMP_CUBEMAPS = 42,
			LUMP_TEXDATA_STRING_DATA = 43,
			LUMP_TEXDATA_STRING_TABLE = 44,
			LUMP_OVERLAYS = 45,
			LUMP_LEAFMINDISTTOWATER = 46,
			LUMP_FACE_MACRO_TEXTURE_INFO = 47,
			LUMP_DISP_TRIS = 48,
			LUMP_PHYSCOLLIDESURFACE = 49,
			LUMP_WATEROVERLAYS = 50,
			LUMP_LEAF_AMBIENT_INDEX_HDR = 51,
			LUMP_LEAF_AMBIENT_INDEX = 52,
			LUMP_LIGHTING_HDR = 53,
			LUMP_WORLDLIGHTS_HDR = 54,
			LUMP_LEAF_AMBIENT_LIGHTING_HDR = 55,
			LUMP_LEAF_AMBIENT_LIGHTING = 56,
			LUMP_XZIPPAKFILE = 57,
			LUMP_FACES_HDR = 58,
			LUMP_MAP_FLAGS = 59,
			LUMP_OVERLAY_FADES = 60,
			LUMP_OVERLAY_SYSTEM_LEVELS  = 61,
			LUMP_PHYSLEVEL  = 62,
			LUMP_DISP_MULTIBLEND = 63
		};

		public string ReadStringInLump(char[] data, int offset) {
			string buffer = "";
			int i = offset;
			while(data[i] != '\0') {
				buffer += data [i];
				i++;
			}
			return buffer;
		}

		public BSPLoader () 
		{
		}

		public dheader_t header {
			get;
			private set;
		}

		public dplane_t[] planes {
			get;
			private set;
		}

		public dnode_t[] bspnodes {
			get;
			private set;
		}

		public dleaf_t[] bspleaves {
			get;
			private set;
		}

		public texinfo_t[] texinfos {
			get;
			private set;
		}

		public dtexdata_t[] texdata {
			get;
			private set;
		}

		public int[] TexdataStringTable {
			get;
			private set;
		}

		public char[] TexdataStringData {
			get;
			private set;
		}

		public void LoadBSP(BinaryReader br) {
			int cnt;
			this.header = new dheader_t (br);
			//Loading planes
			cnt = (this.header.lumps [(int)lumpTypes.LUMP_PLANES].filelen) / STRUCT_PLANE_SIZE;
			planes=new dplane_t[cnt];
			br.BaseStream.Seek(this.header.lumps [(int)lumpTypes.LUMP_PLANES].fileofs, SeekOrigin.Begin);
			for(int i=0 ; i<cnt ; i++) {
				planes [i] = new dplane_t (br);
			}
			//Loading BSP nodes
			cnt = (this.header.lumps [(int)lumpTypes.LUMP_NODES].filelen) / STRUCT_BSPNODE_SIZE;
			bspnodes=new dnode_t[cnt];
			br.BaseStream.Seek(this.header.lumps [(int)lumpTypes.LUMP_NODES].fileofs, SeekOrigin.Begin);
			for(int i=0 ; i<cnt ; i++) {
				bspnodes [i] = new dnode_t (br);
			}
			//Loading BSP leaves
			cnt = (this.header.lumps [(int)lumpTypes.LUMP_LEAFS].filelen) / STRUCT_BSPLEAF_SIZE;
			bspleaves=new dleaf_t[cnt];
			br.BaseStream.Seek(this.header.lumps [(int)lumpTypes.LUMP_LEAFS].fileofs, SeekOrigin.Begin);
			for(int i=0 ; i<cnt ; i++) {
				bspleaves [i] = new dleaf_t (br);
			}
			//Loading texinfo
			cnt = (this.header.lumps [(int)lumpTypes.LUMP_TEXINFO].filelen) / STRUCT_TEXINFO_SIZE;
			texinfos=new texinfo_t[cnt];
			br.BaseStream.Seek(this.header.lumps [(int)lumpTypes.LUMP_TEXINFO].fileofs, SeekOrigin.Begin);
			for(int i=0 ; i<cnt ; i++) {
				texinfos [i] = new texinfo_t (br);
			}

			//Loading texdata
			cnt = (this.header.lumps [(int)lumpTypes.LUMP_TEXDATA].filelen) / STRUCT_TEXDATA_SIZE;
			texdata=new dtexdata_t[cnt];
			br.BaseStream.Seek(this.header.lumps [(int)lumpTypes.LUMP_TEXDATA].fileofs, SeekOrigin.Begin);
			for(int i=0 ; i<cnt ; i++) {
				texdata [i] = new dtexdata_t (br);
			}
			//Loading TexdataStringTable
			cnt = (this.header.lumps [(int)lumpTypes.LUMP_TEXDATA_STRING_TABLE].filelen) / 4;
			TexdataStringTable=new int[cnt];
			br.BaseStream.Seek(this.header.lumps [(int)lumpTypes.LUMP_TEXDATA_STRING_TABLE].fileofs, SeekOrigin.Begin);
			for(int i=0 ; i<cnt ; i++) {
				TexdataStringTable [i] = br.ReadInt32();
			}
			//Loading TexdataStringData
			cnt = (this.header.lumps [(int)lumpTypes.LUMP_TEXDATA_STRING_DATA].filelen) / 1;
			br.BaseStream.Seek(this.header.lumps [(int)lumpTypes.LUMP_TEXDATA_STRING_DATA].fileofs, SeekOrigin.Begin);
			TexdataStringData=br.ReadChars(cnt);
		}
	}
}

