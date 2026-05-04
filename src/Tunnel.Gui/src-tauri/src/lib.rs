use std::fs;
use tauri::{
    menu::{Menu, MenuItem},
    tray::TrayIconBuilder,
    Manager,
};

#[tauri::command]
fn get_auth_token() -> Result<String, String> {
    let mut auth_path = dirs::home_dir().ok_or("Không tìm thấy Home Directory")?;
    auth_path.push(".tunnel");
    auth_path.push(".auth");
    
    fs::read_to_string(auth_path)
        .map(|s| s.trim().to_string())
        .map_err(|e| format!("Lỗi khi đọc file .auth: {}", e))
}

#[tauri::command]
fn start_daemon() -> Result<String, String> {
    #[cfg(target_os = "windows")]
    {
        // On Windows, use Command to start tunnel-daemon.exe
        use std::os::windows::process::CommandExt;
        const CREATE_NO_WINDOW: u32 = 0x08000000;
        
        let mut cmd = std::process::Command::new("tunnel-daemon.exe");
        cmd.creation_flags(CREATE_NO_WINDOW);
        
        match cmd.spawn() {
            Ok(_) => Ok("Started tunnel-daemon.exe on Windows".to_string()),
            Err(e) => Err(format!("Lỗi khi khởi chạy tunnel-daemon.exe: {}", e)),
        }
    }

    #[cfg(not(target_os = "windows"))]
    {
        // On Linux, use systemctl
        match std::process::Command::new("systemctl")
            .args(["--user", "start", "tunnel"])
            .spawn() 
        {
            Ok(_) => Ok("Started tunnel daemon via systemctl".to_string()),
            Err(e) => Err(format!("Lỗi khi chạy systemctl: {}", e)),
        }
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .setup(|app| {
            if cfg!(debug_assertions) {
                app.handle().plugin(
                    tauri_plugin_log::Builder::default()
                        .level(log::LevelFilter::Info)
                        .build(),
                )?;
            }

            // Tạo các MenuItem
            let open_i = MenuItem::with_id(app, "open", "Mở GUI", true, None::<&str>)?;
            let reload_i = MenuItem::with_id(app, "reload", "Reload Config", true, None::<&str>)?;
            let disconnect_i = MenuItem::with_id(app, "disconnect", "Ngắt kết nối Tunnel", true, None::<&str>)?;
            let quit_i = MenuItem::with_id(app, "quit", "Thoát", true, None::<&str>)?;
            
            // Khởi tạo Menu cho Tray
            let menu = Menu::with_items(app, &[&open_i, &reload_i, &disconnect_i, &quit_i])?;
            
            // Xây dựng System Tray
            let _tray = TrayIconBuilder::new()
                .menu(&menu)
                .show_menu_on_left_click(true)
                .icon(app.default_window_icon().unwrap().clone())
                .on_menu_event(|app, event| match event.id.as_ref() {
                    "open" => {
                        if let Some(window) = app.get_webview_window("main") {
                            window.show().unwrap();
                            window.set_focus().unwrap();
                        }
                    }
                    "reload" => {
                        // Gửi Emit hoặc tự xử lý gọi sang Daemon
                        println!("Đã chọn Reload Config");
                    }
                    "disconnect" => {
                        println!("Đã chọn Ngắt kết nối Tunnel");
                    }
                    "quit" => {
                        app.exit(0);
                    }
                    _ => {}
                })
                .build(app)?;

            Ok(())
        })
        .on_window_event(|window, event| match event {
            tauri::WindowEvent::CloseRequested { api, .. } => {
                // Ẩn cửa sổ xuống System Tray thay vì thoát hẳn
                api.prevent_close();
                window.hide().unwrap();
            }
            _ => {}
        })
        .invoke_handler(tauri::generate_handler![get_auth_token, start_daemon])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
