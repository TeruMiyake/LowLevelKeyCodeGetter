# LowLevelKeyCodeGetter

## 概要
WH_KEYBOARD_LLを使って各種KEYDOWN/KEYUPイベントをグローバルに（=thread-specificでなく）フックし、リアルタイムにログを表示します。

## Windows の入力の流れ

* [Keyboard Input](https://learn.microsoft.com/en-us/windows/win32/inputdev/keyboard-input)
    * Keyboard Input についてのポータルページ
* [Keyboard Input Overview](https://learn.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input)
    * キーコードや入力メッセージ（WM_KEYDOWNなど）について詳しい。キーボードインプットについて概観しているが、WH_KEYBOARD_LL の話は出てこない。
    * ウィンドウのフォーカスなどについて出てくる
* [Using Keyboard Input](https://learn.microsoft.com/en-us/windows/win32/inputdev/using-keyboard-input)
    * ウィンドウのプロシージャが WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, and WM_SYSKEYUP を受け取ってから表示するまでを書いてある
* [About Messages and Message Queues](https://learn.microsoft.com/en-us/windows/win32/winmsg/about-messages-and-message-queues)
    * メッセージについて詳しい


### WH_KEYBOARD vs WH_KEYBOARD_LL
以下、上記の参考文献（Microsoft Learn）より引用。

* WH_KEYBOARD_LL
    * フックを扱うプログラム内に定義したコールバックを（恐らくフックを扱うプログラム内で？）実行する
    > The WH_KEYBOARD_LL hook enables you to monitor keyboard input events about to be posted in a thread input queue.
    > For more information, see the LowLevelKeyboardProc callback function.
* WH_KEYBOARD
    * DLL を作成して、そのDLLをフックされたプロセスに注入する必要がある。
    > The WH_KEYBOARD hook enables an application to monitor message traffic for WM_KEYDOWN and WM_KEYUP messages about to be returned by the GetMessage or PeekMessage function. You can use the WH_KEYBOARD hook to monitor keyboard input posted to a message queue.
    > For more information, see the KeyboardProc callback function.


### Hooks - global hook vs thread-specific hook

* SetWindowsHookEx() の引数 dwThreadId に 0 を渡した場合は global となり、特定のスレッド ID を指定すれば thread-specific となる？
* WH_KEYBOARD_LL と WH_KEYBOARD の違いは関係ない
* global hook
    > A global hook monitors messages for all threads in the same desktop as the calling thread.

    > A global hook procedure can be called in the context of any application in the same desktop as the calling thread, so the procedure must be in a separate DLL module.

    > You should use global hooks only for debugging purposes; otherwise, you should avoid them. Global hooks hurt system performance and cause conflicts with other applications that implement the same type of global hook.
* thread-specific hook
    > A thread-specific hook monitors messages for only an individual thread.

    > A thread-specific hook procedure is called only in the context of the associated thread.
    * 同じアプリケーション内のスレッドにフックする場合
        > You should use global hooks only for debugging purposes; otherwise, you should avoid them. Global hooks hurt system performance and cause conflicts with other applications that implement the same type of global hook.
    * 異なるアプリケーション内のスレッドにフックする場合
        > If the application installs a hook procedure for a thread of a different application, the procedure must be in a DLL.