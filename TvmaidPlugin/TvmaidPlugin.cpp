#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Windows ヘッダーから使用されていない部分を除外します。
#include <windows.h>

#include <stdlib.h>
#include <process.h>
#include <memory.h>
#include <locale.h>
#include <wincon.h>
#include <stdio.h>
#include <string.h>

#define TVTEST_PLUGIN_CLASS_IMPLEMENT	// プラグインをクラスとして実装
#include "TVTestPlugin.h"
using namespace TVTest;

typedef WCHAR wchar;
const int null = 0;

wchar* PluginVer = L"Tvmaid YUI Plugin R1";

//例外
class Exception
{
public:
	enum ErrorCode
	{
		NoError = 0,
		CreateShared,
		CreateWindow,
		CreateMutex,
		StartRec,
		StopRec,
		SetService,
		GetEvents,
		GetState,
		GetEnv,
		GetEventTime,
		GetTsStatus,
		GetLogo
	};

	ErrorCode Code;
	wchar* Message;	//動的メモリを割り当てないこと。固定メッセージのみ

	Exception(ErrorCode code, wchar* msg)
	{
		this->Code = code;
		this->Message = msg;
	}
};

//共有メモリ
class Shared
{
	HANDLE map;			//メモリマップハンドル
	wchar* view;		//メモリマップのポインタ
	const int memSize;	//全体のサイズ(文字数)
	size_t off;

public:
	Shared(wchar* name, int size) : off(0), memSize(size)
	{
		map = CreateFileMappingW((HANDLE)INVALID_HANDLE_VALUE, null, PAGE_READWRITE, 0, memSize * sizeof(wchar), name);
		if (map == null)
		{
			throw Exception(Exception::CreateShared, L"共有メモリの作成に失敗しました。");
		}

		view = (wchar*)MapViewOfFile(map, FILE_MAP_ALL_ACCESS, 0, 0, 0);
		if (view == null)
		{
			CloseHandle(map);
			throw Exception(Exception::CreateShared, L"共有メモリの作成に失敗しました。");
		}
	}

	~Shared()
	{
		UnmapViewOfFile(view);
		CloseHandle(map);
	}

	wchar* Read()
	{
		return view;
	}

	void Reset()
	{
		off = 0;
	}

	bool Write(wchar* format, ...)
	{
		if ((memSize - off) == 0)
			return false;
		va_list args;
		va_start(args, format);
		int len = _vsnwprintf_s(view + off, memSize - off, _TRUNCATE, format, args);
		va_end(args);
		if (len == -1)
		{
			*(view + off) = L'\0';
			return false;
		}
		off += len;
		return true;
	}
};

//ミューテックス
class Mutex
{
	HANDLE mutex;

public:
	Mutex(wchar* name)
	{
		mutex = CreateMutexW(null, FALSE, name);
		if (mutex == null)
		{
			throw Exception(Exception::CreateMutex, L"Mutexの作成に失敗しました。");
		}
	}
	
	bool GetOwner(int timeout)
	{
		DWORD err = WaitForSingleObject(mutex, timeout);
		return (err == WAIT_ABANDONED || err == WAIT_OBJECT_0);
	}

	~Mutex()
	{
		if (mutex != null)
		{
			ReleaseMutex(mutex);
			CloseHandle(mutex);
			mutex = null;
		}
	}
};

//通信用ウインドウ
class Window
{
	HWND window;

public:
	Window(WNDPROC proc, wchar* id, LPVOID data) : window(null)
	{
		const wchar* wndClass = L"/tvmaid/win";

		WNDCLASSW wc;
		wc.style = 0;
		wc.lpfnWndProc = proc;
		wc.cbClsExtra = 0;
		wc.cbWndExtra = 0;
		wc.hInstance = g_hinstDLL;
		wc.hIcon = null;
		wc.hCursor = null;
		wc.hbrBackground = null;
		wc.lpszMenuName = null;
		wc.lpszClassName = wndClass;

		if (GetClassInfoW(g_hinstDLL, wndClass, &wc) == 0)	//クラスが登録済みか
		{
			if (RegisterClassW(&wc) == 0)
			{
				throw Exception(Exception::CreateWindowW, L"ウインドウの作成に失敗しました。");
			}
		}

		window = CreateWindowW(wndClass, id, 0, 0, 0, 0, 0, HWND_MESSAGE, null, g_hinstDLL, data);
		if (window == null)
		{
			throw Exception(Exception::CreateWindow, L"ウインドウの作成に失敗しました。");
		}
	}

	~Window()
	{
		if (window != null)
		{
			DestroyWindow(window);
			window = null;
		}
	}
};

//プラグインクラスの基底クラス
//制御用ウインドウ、メモリマップ、Mutexの管理を行う
class TvProcessBase : public CTVTestPlugin
{
private:
	Window* window;					//制御用ウインドウ
	Mutex* mutex;					//チューナ排他制御Mutex。このMutexは、複数のTVTest間でチューナの排他制御をするためのものなので、他で所有権を取得しないこと
	wchar driverId[MAX_PATH];		//ドライバID
	bool userStart = true;			//起動したのがユーザかどうか
	const int timeout = 60 * 1000;	//ドライバ開放待ち時間(Mutex)
	bool initialized = false;

protected:
	Shared* shared_in;				//共有メモリ(入力)
	Shared* shared_out;				//共有メモリ(出力)

private:
	virtual void Call(UINT callNum, WPARAM arg1, LPARAM arg2) = 0;

	//TVTestから直接呼ばれるメソッド群
	//例外を必ず処理する
	virtual bool GetPluginInfo(TVTest::PluginInfo *pInfo)
	{
		pInfo->Type = PLUGIN_TYPE_NORMAL;
		pInfo->Flags = 0;
		pInfo->pszPluginName = L"Tvmaid YUI Plugin";
		pInfo->pszCopyright = L"Tvmaid";
		pInfo->pszDescription = L"Tvmaid YUI Plugin";
		return true;
	}

	virtual bool Initialize()
	{
		/*
		::AllocConsole();
		_wfreopen(L"CON", L"r", stdin);
		_wfreopen(L"CON", L"w", stdout);
		_wsetlocale(LC_ALL, L"japanese");
		*/
		Log(PluginVer);
		window = null;
		mutex = null;
		shared_in = shared_out = null;
		m_pApp->SetEventCallback(EventCallback, this);
		return true;
	}

	virtual bool Finalize()
	{
		Dispose();
		return true;
	}

	//TVTestイベントコールバック
	static LRESULT CALLBACK EventCallback(UINT Event, LPARAM lParam1, LPARAM lParam2, void *pClientData)
	{
		TvProcessBase* _this = static_cast<TvProcessBase*>(pClientData);

		switch (Event)
		{
		case EVENT_DRIVERCHANGE:
			//ユーザがドライバを変更した
			try
			{
				_this->userStart = true;	//ユーザが起動したことにする
				_this->Dispose();
				_this->Init(null);
			}
			catch (Exception& e)
			{
				_this->Log(e.Message);
				_this->Dispose();
			}
			break;

		case EVENT_STARTUPDONE:
			_this->Debug(L"Startup done");
			_this->m_pApp->EnablePlugin(true);
			break;

		case EVENT_PLUGINENABLE:
			try
			{
				_this->Debug(L"Enable Plugin");
				_this->EnablePlugin(lParam1 != 0);
			}
			catch (Exception& e)
			{
				_this->Log(e.Message);
				_this->Dispose();
				//Tvmaidからの起動なら終了する
				if (_this->userStart == false)
					_this->m_pApp->Close();
			}
			break;
		}
		return 0;
	}

	//通信用ウインドウコールバック
	static LRESULT CALLBACK WndProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
	{
		//Tvmaidからの呼び出し
		//エラーコードを返す
		if (uMsg >= 0xb000)
		{
			try
			{
				TvProcessBase* _this = reinterpret_cast<TvProcessBase*>(GetWindowLongPtr(hwnd, GWLP_USERDATA));
				_this->Call(uMsg - 0xb000, wParam, lParam);
				return 0;	//エラー無し
			}
			catch (Exception& e)
			{
				return e.Code;	//エラーコードを返す
			}
		}
		else
		{
			switch (uMsg)
			{
			case WM_CREATE:
				LPCREATESTRUCT pcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
				TvProcessBase* _this = reinterpret_cast<TvProcessBase*>(pcs->lpCreateParams);
				SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(_this));
				return 0;	//0を返すこと
			}
			return DefWindowProc(hwnd, uMsg, wParam, lParam);
		}
	}

	void Dispose()
	{
		if (window != null)
		{
			delete window;
			window = null;
		}
		if (mutex != null)
		{
			delete mutex;
			mutex = null;
		}
		if (shared_in != null)
		{
			delete shared_in;
			shared_in = null;
		}
		if (shared_out != null)
		{
			delete shared_out;
			shared_out = null;
		}
		initialized = false;
	}

	void EnablePlugin(bool enable)
	{
		if (enable)
		{
			//環境変数で渡されている場合は、そのDriverIdを使う
			size_t size;
			_wgetenv_s(&size, null, 0, L"DriverId");
			if (size == 0)
			{
				Debug(L"ユーザが起動しました。");
				userStart = true;
				Init(null);
			}
			else
			{
				wchar env[MAX_PATH];
				if (_wgetenv_s(&size, env, L"DriverId") != 0)
				{
					throw Exception(Exception::GetEnv, L"環境変数の取得に失敗しました。");
				}
				else
				{
					Debug(L"Tvmaidから起動しました。");
					userStart = false;
					Init(env);
				}
			}
		}
		else
		{
			Dispose();
		}
	}

	void Init(wchar* id)
	{
		if (initialized)
			return;

		id == null ? InitMutex() : InitMutex(id);

		wchar name[MAX_PATH];
		swprintf_s(name, L"/tvmaid/map/in/%s", driverId);
		shared_in = new Shared(name, 1024);
		swprintf_s(name, L"/tvmaid/map/out/%s", driverId);
		shared_out = new Shared(name, 2 * 1024 * 1024);

		window = new Window(WndProc, driverId, this);	//ウインドウをメモリマップより後に作る。これで通信可能か判断しているため

		wchar msg[MAX_PATH];
		wcscpy_s(msg, L"Tvmaid DriverId: ");
		wcscat_s(msg, driverId);
		Log(msg);

		initialized = true;
	}

	void InitMutex(wchar* id)
	{
		wchar name[MAX_PATH];
		swprintf_s(name, L"/tvmaid/mutex/%s", id);

		mutex = new Mutex(name);
		if (mutex->GetOwner(timeout))
		{ 
			wcscpy_s(driverId, id);
		}
		else
		{
			throw Exception(Exception::CreateMutex, L"ミューテックスの作成に失敗しました。");
		}
	}

	//ドライバ名を取得
	void GetDriver(wchar* driver)
	{
		wchar path[MAX_PATH];
		wchar ext[MAX_PATH];

		m_pApp->GetDriverFullPathName(path, MAX_PATH);
		_wsplitpath_s(path, null, 0, null, 0, driver, MAX_PATH, ext, MAX_PATH);

		wcscat_s(driver, MAX_PATH, ext);
	}

	void InitMutex()
	{
		wchar driver[MAX_PATH];
		GetDriver(driver);

		int i = 0;
		while (1)
		{
			wchar name[MAX_PATH];

			swprintf_s(driverId, L"%s/%d", driver, i);
			swprintf_s(name, L"/tvmaid/mutex/%s", driverId);

			mutex = new Mutex(name);
			if(mutex->GetOwner(0))
			{
				break;
			}
			else
			{
				i++;
			}
		}
	}

	void Log(wchar* msg)
	{
		m_pApp->AddLog(msg);
	}

	void Debug(wchar* msg)
	{
#ifdef _DEBUG
		m_pApp->AddLog(msg);
#endif
	}
};

//プラグインクラス
class TvProcess : public TvProcessBase
{
	virtual void Call(UINT callNum, WPARAM arg1, LPARAM arg2)
	{
		typedef void(TvProcess::* TvProcessMFP)();
		static const TvProcessMFP call[] =
		{
			&TvProcess::Close,
			&TvProcess::GetState,
			&TvProcess::GetServices,
			&TvProcess::GetEvents,
			&TvProcess::SetService,
			&TvProcess::StartRec,
			&TvProcess::StopRec,
			&TvProcess::GetEventTime,
			&TvProcess::GetTsStatus,
			&TvProcess::GetLogo
		};

		if (_countof(call) > callNum)
		{
			shared_out->Reset();
			(this->*call[callNum])();
		}
	}

	void GetTimeStr(SYSTEMTIME* time, wchar* buf, size_t size)
	{
		swprintf_s(buf, size, L"%d/%d/%d %02d:%02d:00",
			time->wYear,
			time->wMonth,
			time->wDay,
			time->wHour,
			time->wMinute);
	}

	void Close()
	{
		if (m_pApp->Close(CLOSE_EXIT) == false)
		{
			throw Exception(Exception::StopRec, L"TVTestの終了に失敗しました。");
		}
	}

	void GetState()
	{
		RecordStatusInfo info;
		info.Size = sizeof(RecordStatusInfo);

		if (m_pApp->GetRecordStatus(&info))
		{
			shared_out->Write(L"%u", info.Status);
		}
		else
		{
			throw Exception(Exception::GetState, L"状態の取得に失敗しました。");
		}
	}

	//サービスのリストを取得する
	void GetServices()
	{
		int num = 0;
		m_pApp->GetTuningSpace(&num);

		// 全てのチューニング空間のサービスを取得する
		for (int space = 0; space < num; space++)
		{
			ChannelInfo info;
			info.Size = sizeof(info);

			for (int ch = 0; m_pApp->GetChannelInfo(space, ch, &info); ch++)
			{
				//有効なサービスだけ取得
				if(info.Flags != CHANNEL_FLAG_DISABLED)
				{
					if (shared_out->Write(L"%u\x2%u\x2%u\x2%s\x1",
						info.NetworkID,
						info.TransportStreamID,
						info.ServiceID,
						info.szChannelName
						) == false)
						break;
				}
			}
		}
	}

	void GetEvents()
	{
		int nid, tsid, sid;
		swscanf_s(shared_in->Read(), L"%d\x1%d\x1%d", &nid, &tsid, &sid);

		EpgEventList list;
		list.NetworkID = nid;
		list.TransportStreamID = tsid;
		list.ServiceID = sid;

		if (m_pApp->GetEpgEventList(&list))
		{
			for (int i = 0; i < list.NumEvents; i++)
			{
				EpgEventInfo* event = list.EventList[i];

				wchar time[30];
				GetTimeStr(&event->StartTime, time, sizeof(time) / sizeof(wchar_t));

				//ジャンルを取得
				unsigned int genre = 0xffffffff;
				if (event->ContentList)
				{
					for (int j = 0; j < event->ContentListLength; j++)
					{
						unsigned int a = ((event->ContentList[j].ContentNibbleLevel1 & 0xf) << 4) | (event->ContentList[j].ContentNibbleLevel2 & 0xf);
						unsigned int b = ~(0xff << (j * 8)) | (a << (j * 8));
						genre &= b;
						if (j == 3)
							break;
					}
				}

				wchar* nullStr = L"";
				if (shared_out->Write(L"%u\x2%s\x2%u\x2%s\x2%s\x2%s\x2%u\x1",
					event->EventID,
					time,
					event->Duration,
					event->pszEventName == null ? nullStr : event->pszEventName,
					event->pszEventText == null ? nullStr : event->pszEventText,
					event->pszEventExtendedText == null ? nullStr : event->pszEventExtendedText,
					genre
					) == false)
					break;
			}
			m_pApp->FreeEpgEventList(&list);
		}
		else
		{
			throw Exception(Exception::GetEvents, L"番組表の取得に失敗しました。");
		}
	}

	//サービス変更
	void SetService()
	{
		int nid, tsid, sid;
		swscanf_s(shared_in->Read(), L"%d\x1%d\x1%d", &nid, &tsid, &sid);

		int num = 0;
		m_pApp->GetTuningSpace(&num);

		for (int space = 0; space < num; space++)
		{
			ChannelInfo info;
			info.Size = sizeof(info);

			for (int ch = 0; m_pApp->GetChannelInfo(space, ch, &info); ch++)
			{
				if (info.Flags != CHANNEL_FLAG_DISABLED && info.NetworkID == nid && info.TransportStreamID == tsid && info.ServiceID == sid)
				{
					if (m_pApp->SetChannel(space, ch) == false)
					{
						throw Exception(Exception::SetService, L"サービスの変更に失敗しました。");
					}
					return;
				}
			}
		}
		throw Exception(Exception::SetService, L"サービスの変更に失敗しました。");
	}

	void StartRec()
	{
		RecordInfo info;
		info.Mask = RECORD_MASK_FILENAME;
		info.Flags = 0;
		info.StartTimeSpec = RECORD_START_NOTSPECIFIED;
		info.StopTimeSpec = RECORD_STOP_NOTSPECIFIED;
		info.pszFileName = shared_in->Read();

		if (m_pApp->StartRecord(&info) == false)
		{
			throw Exception(Exception::StartRec, L"録画を開始できません。");
		}
	}

	void StopRec()
	{
		if (m_pApp->StopRecord() == false)
		{
			throw Exception(Exception::StopRec, L"録画を停止できません。");
		}
	}

	//指定番組の時間を取得(録画用時、時間チェック用)
	void GetEventTime()
	{
		int nid, tsid, sid, eid;
		swscanf_s(shared_in->Read(), L"%d\x1%d\x1%d\x1%d", &nid, &tsid, &sid, &eid);

		EpgEventQueryInfo info;
		info.NetworkID = nid;
		info.TransportStreamID = tsid;
		info.ServiceID = sid;
		info.EventID = eid;
		info.Type = EPG_EVENT_QUERY_EVENTID;
		info.Flags = 0;
		EpgEventInfo* event = m_pApp->GetEpgEventInfo(&info);

		if (event != null)
		{
			wchar time[30];
			GetTimeStr(&event->StartTime, time, sizeof(time) / sizeof(wchar_t));
			shared_out->Write(L"%s\x1%u", time, event->Duration);
			m_pApp->FreeEpgEventInfo(event);
		}
		else
		{
			throw Exception(Exception::GetEventTime, L"番組時間の取得に失敗しました。");
		}
	}

	void GetTsStatus()
	{
		StatusInfo info;
		info.Size = sizeof(StatusInfo);

		if (m_pApp->GetStatus(&info))
		{
			shared_out->Write(L"%u\x1%u\x1%u", info.ErrorPacketCount, info.DropPacketCount, info.ScramblePacketCount);
		}
		else
		{
			throw Exception(Exception::GetTsStatus, L"TS状態の取得に失敗しました。");
		}
	}

	void GetLogo()
	{
		int nid, tsid, sid;
		wchar path[MAX_PATH];
		swscanf_s(shared_in->Read(), L"%d\x1%d\x1%d\x1%s", &nid, &tsid, &sid, path, _countof(path));

		HBITMAP hBm = m_pApp->GetLogo(nid, sid, 2);
		if (hBm)
		{
			BITMAP bm;
			if (GetObject(hBm, sizeof(bm), &bm) == sizeof(bm))
			{
				const DWORD dwSize = sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + (bm.bmWidthBytes * bm.bmHeight);
				BYTE *p = new BYTE[dwSize];
				BITMAPFILEHEADER *pBfh = reinterpret_cast<BITMAPFILEHEADER *>(p);
				BITMAPINFOHEADER *pBih = reinterpret_cast<BITMAPINFOHEADER *>(pBfh + 1);
				BYTE *pPixel = reinterpret_cast<BYTE *>(pBih + 1);

				pBfh->bfType = 0x4d42;	// "BM"
				pBfh->bfSize = dwSize;
				pBfh->bfReserved1 = 0;
				pBfh->bfReserved2 = 0;
				pBfh->bfOffBits = sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER);

				pBih->biSize = sizeof(BITMAPINFOHEADER);
				pBih->biWidth  = bm.bmWidth;
				pBih->biHeight = bm.bmHeight;
				pBih->biPlanes = 1;
				pBih->biBitCount = bm.bmBitsPixel;	// always 32
				pBih->biCompression = BI_RGB;
				pBih->biSizeImage = 0;
				pBih->biXPelsPerMeter = 0;
				pBih->biYPelsPerMeter = 0;
				pBih->biClrUsed = 0;
				pBih->biClrImportant = 0;

				HDC hDC = GetDC(NULL);
				if (hDC)
				{
					GetDIBits(hDC, hBm, 0, bm.bmHeight, pPixel, (LPBITMAPINFO)pBih, DIB_RGB_COLORS);
					ReleaseDC(NULL, hDC);
					HANDLE hFile = CreateFileW(path, GENERIC_WRITE, FILE_SHARE_WRITE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
					if (hFile != INVALID_HANDLE_VALUE)
					{
						DWORD dw;
						WriteFile(hFile, p, dwSize, &dw, NULL);
						CloseHandle(hFile);
					}
				}
				delete[] p;
			}
			DeleteObject(hBm);
		}
		else
		{
			throw Exception(Exception::GetLogo, L"ロゴの取得に失敗しました。");
		}
	}
};

CTVTestPlugin* CreatePluginClass()
{
	return new TvProcess;
}
