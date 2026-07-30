#undef BYTE

#include "libImmCore/src/libBasics/piVecTypes.h"
#include "libImmCore/src/libBasics/piImage.h"
#include "libImmCore/src/libBasics/piFile.h"
#include "libImmCore/src/libBasics/piStr.h"
#include "layerPicture.h"

using namespace ImmCore;

namespace ImmImporter
{
	LayerPicture::LayerPicture() {}

	LayerPicture::~LayerPicture() {}

	bool LayerPicture::Init(ContentType type, bool isViewerLocked, piLog *log)
	{
		mGpuId = -1;
		mType = type;
		mIsViewerLocked = isViewerLocked;
		mImage.Init();

		return true;
	}
    
    bool LayerPicture::LoadAssetMemory(const piTArray<uint8_t> & data, piLog *log, const wchar_t *ext)
    {
        if( !mImage.ReadFromMemory(&data, ext) )
            return false;

        // The renderer's upload path accepts ONLY FORMAT_I_GREY and
        // FORMAT_I_RGBA (layerRendererPicture.cpp iUpload) and rejects anything
        // else through an unlogged `default: return false`. Convert() fails by
        // returning false WITHOUT changing the format (piImage.cpp:432-437 -
        // iConvertSelf returns null on allocation failure), so a discarded
        // return value here means the layer is silently undrawable forever:
        // every frame it is visible, iUpload rejects it and DisplayRender skips
        // it, with no diagnostic anywhere.
        //
        // This is not hypothetical for large images. An 8192x4096 three-channel
        // JPEG needs a 128 MiB allocation to become RGBA.
        if (!mImage.Convert( 0, piImage::Format::FORMAT_I_RGBA, false ))
        {
            if (log)
                log->Printf(LT_ERROR, L"[IMM_PICFMT] RGBA conversion FAILED for a picture asset (%dx%d, format %d) - it will never upload and never draw",
                    mImage.GetXRes(), mImage.GetYRes(), (int)mImage.GetFormat(0));
            return false;
        }

        iComputeBBox();
        return true;
    }

	void LayerPicture::Deinit(void)
	{
		mImage.Free();
	}

	void LayerPicture::SetGpuId(int id)
	{
		mGpuId = id;
	}

	bool LayerPicture::GetIsViewerLocked(void) const
	{
		return mIsViewerLocked;
	}

	piImage * LayerPicture::GetImage(void)
	{
		return &mImage;
	}


	int LayerPicture::GetGpuId(void) const { return mGpuId; }


	void LayerPicture::iComputeBBox(void)
	{
		if (mType == Image2D)
		{
			const float ar = float(mImage.GetXRes()) / float(mImage.GetYRes());
			const vec3 rad = vec3(ar, 1.0f, 0.05f);
			mBBox = bound3(-rad.x, rad.x, -rad.y, rad.y, -rad.z, rad.z);
		}
		else // Cubemaps and Equirect spheres
		{
			mBBox = bound3(-1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f);
		}
	}

	LayerPicture::ContentType LayerPicture::GetType(void) const
	{
		return mType;
	}

	const bool LayerPicture::HasBBox(void) const
	{
		return true;
	}
	const bound3 & LayerPicture::GetBBox(void) const
	{
		return mBBox;
	}

}
