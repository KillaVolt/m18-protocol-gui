import sys
import subprocess
import threading
import io
import time
import traceback
import tkinter as tk
from tkinter import ttk, messagebox

def ensure_module(mod_name: str, pip_name: str) -> None:
    try:
        __import__(mod_name)
        return
    except ImportError:
        pass
    install_root = tk.Tk()
    install_root.title("M18 GUI - Installing dependencies")
    install_root.geometry("420x120")
    label = ttk.Label(
        install_root,
        text=(
            f"Installing required Python package:\n{pip_name}\n\n"
            "This may take a moment..."
        ),
        justify="center"
    )
    label.pack(padx=10, pady=10)
    progress = ttk.Progressbar(install_root, mode="indeterminate")
    progress.pack(fill="x", padx=10, pady=(0, 10))
    progress.start(10)
    install_root.update_idletasks()
    try:
        subprocess.check_call(
            [sys.executable, "-m", "pip", "install", pip_name],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL
        )
    except subprocess.CalledProcessError as e:
        progress.stop()
        messagebox.showerror(
            "Installation error",
            f"Failed to install '{pip_name}'.\n\nError code: {e.returncode}"
        )
        install_root.destroy()
        sys.exit(1)
    progress.stop()
    install_root.destroy()
    try:
        __import__(mod_name)
    except ImportError:
        messagebox.showerror(
            "Import error",
            f"Package '{pip_name}' was installed, but could not be imported.\n"
            f"Please restart Python or install it manually."
        )
        sys.exit(1)

ensure_module("serial", "pyserial")
ensure_module("requests", "requests")

from serial.tools import list_ports
import m18

class M18GUI(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("M18 Protocol GUI")
        self.m18_obj = None
        self.sim_thread = None
        self.sim_stop_event = None
        self.sim_profiles = {"Gentle": (150, 2500), "Normal": (300, 6000), "Aggressive": (450, 9000)}
        self.custom_cutoff_raw_var = tk.StringVar(value="300")
        self.custom_cutoff_amps_var = tk.StringVar(value="0.30")
        self.custom_max_raw_var = tk.StringVar(value="6000")
        self.custom_max_amps_var = tk.StringVar(value="6.00")
        self._sync_in_progress = False
        self.port_map = {}
        self.create_widgets()
        self.refresh_ports()
        self.update_profile_display()

    def create_widgets(self):
        self.notebook = ttk.Notebook(self)
        self.notebook.pack(fill="x", expand=False, padx=10, pady=(5, 0))
        self.main_tab = ttk.Frame(self.notebook)
        self.console_tab = ttk.Frame(self.notebook)
        self.sim_tab = ttk.Frame(self.notebook)
        self.about_tab = ttk.Frame(self.notebook)
        self.notebook.add(self.main_tab, text="Main Controls")
        self.notebook.add(self.console_tab, text="Interactive Console")
        self.notebook.add(self.sim_tab, text="Simulation")
        self.notebook.add(self.about_tab, text="About")
        top_frame = ttk.Frame(self.main_tab)
        top_frame.pack(fill="x", padx=5, pady=(5, 0))
        ttk.Label(top_frame, text="Serial port:").grid(row=0, column=0, sticky="w")
        self.port_var = tk.StringVar()
        self.port_combo = ttk.Combobox(top_frame, textvariable=self.port_var, state="readonly", width=55)
        self.port_combo.grid(row=0, column=1, padx=5, sticky="w")
        ttk.Button(top_frame, text="Refresh", command=self.refresh_ports).grid(row=0, column=2, padx=4)
        self.connect_btn = ttk.Button(top_frame, text="Connect", command=self.connect_device)
        self.connect_btn.grid(row=0, column=3, padx=4)
        self.disconnect_btn = ttk.Button(top_frame, text="Disconnect", command=self.disconnect_device, state="disabled")
        self.disconnect_btn.grid(row=0, column=4, padx=4)
        self.status_var = tk.StringVar(value="Not connected")
        ttk.Label(top_frame, textvariable=self.status_var).grid(
            row=1, column=0, columnspan=5, sticky="w", pady=(4, 0)
        )
        mid_frame = ttk.LabelFrame(self.main_tab, text="Commands")
        mid_frame.pack(fill="x", padx=5, pady=(6, 4))
        self.idle_btn = ttk.Button(
            mid_frame, text="Idle (TX low, safe to connect)", command=self.cmd_idle, state="disabled"
        )
        self.idle_btn.grid(row=0, column=0, padx=3, pady=4, sticky="w")
        self.health_btn = ttk.Button(
            mid_frame, text="Health report", command=self.cmd_health, state="disabled"
        )
        self.health_btn.grid(row=0, column=1, padx=3, pady=4, sticky="w")
        self.clipboard_btn = ttk.Button(
            mid_frame, text="Copy all registers to clipboard", command=self.cmd_clipboard, state="disabled"
        )
        self.clipboard_btn.grid(row=0, column=2, padx=3, pady=4, sticky="w")
        # --- Interactive Console
        console_frame = ttk.Frame(self.console_tab)
        console_frame.pack(fill="both", expand=True, padx=5, pady=5)
        ttk.Label(
            console_frame,
            text="Python console environment.\nYou can use 'm' for the connected M18 object.\nExample: m.read_id() or m.health()"
        ).pack(anchor="w")
        self.console_text = tk.Text(console_frame, height=8, wrap="none")
        self.console_text.pack(fill="both", expand=True, padx=5, pady=5)
        console_btn_frame = ttk.Frame(console_frame)
        console_btn_frame.pack(fill="x", padx=5, pady=(0, 5))
        self.console_run_btn = ttk.Button(console_btn_frame, text="Execute", command=self.run_console_code)
        self.console_run_btn.pack(side="left", padx=5)
        self.console_clear_btn = ttk.Button(
            console_btn_frame, text="Clear input", command=lambda: self.console_text.delete("1.0", tk.END)
        )
        self.console_clear_btn.pack(side="left", padx=5)
        # --- Simulation
        sim_main_frame = ttk.Frame(self.sim_tab)
        sim_main_frame.pack(fill="x", padx=6, pady=6)
        ttk.Label(sim_main_frame, text="Simulation duration (seconds):").grid(row=0, column=0, sticky="w", padx=4, pady=2)
        self.sim_duration_var = tk.StringVar(value="60")
        self.sim_duration_entry = ttk.Entry(sim_main_frame, textvariable=self.sim_duration_var, width=10)
        self.sim_duration_entry.grid(row=0, column=1, sticky="w", padx=4, pady=2)
        ttk.Label(sim_main_frame, text="Simulation baudrate (conceptual):").grid(row=1, column=0, sticky="w", padx=4, pady=2)
        self.sim_baud_var = tk.StringVar(value="4800")
        self.sim_baud_combo = ttk.Combobox(
            sim_main_frame, textvariable=self.sim_baud_var, state="readonly", width=10,
            values=["1200", "2400", "4800", "9600"]
        )
        self.sim_baud_combo.grid(row=1, column=1, sticky="w", padx=4, pady=2)
        ttk.Label(
            sim_main_frame,
            text="Note: actual UART to the pack always runs at 4800 baud.\nThis setting only affects how often keepalive packets are sent\nduring simulation. No real charging current flows from USB."
        ).grid(row=2, column=0, columnspan=3, sticky="w", padx=4, pady=(2, 8))
        ttk.Label(sim_main_frame, text="Charger profile:").grid(row=3, column=0, sticky="w", padx=4, pady=2)
        self.sim_profile_var = tk.StringVar(value="Normal")
        profile_values = ["Gentle", "Normal", "Aggressive", "Custom"]
        self.sim_profile_combo = ttk.Combobox(
            sim_main_frame, textvariable=self.sim_profile_var, state="readonly", width=12, values=profile_values
        )
        self.sim_profile_combo.grid(row=3, column=1, sticky="w", padx=4, pady=2)
        self.sim_profile_combo.bind("<<ComboboxSelected>>", self.on_profile_changed)
        self.profile_info_var = tk.StringVar(value="Profile parameters will appear here.")
        ttk.Label(sim_main_frame, textvariable=self.profile_info_var, foreground="blue").grid(
            row=4, column=0, columnspan=3, sticky="w", padx=4, pady=(0, 6)
        )
        self.custom_frame = ttk.LabelFrame(self.sim_tab, text="Custom charger parameters")
        header_frame = ttk.Frame(self.custom_frame)
        header_frame.pack(fill="x", padx=5, pady=(5, 0))
        ttk.Label(header_frame, text="").grid(row=0, column=0, padx=5)
        ttk.Label(header_frame, text="Raw units").grid(row=0, column=1, padx=5)
        ttk.Label(header_frame, text="Amps").grid(row=0, column=2, padx=5)
        rows_frame = ttk.Frame(self.custom_frame)
        rows_frame.pack(fill="x", padx=5, pady=5)
        ttk.Label(rows_frame, text="Cutoff:").grid(row=0, column=0, padx=5, pady=3, sticky="e")
        self.custom_cutoff_raw_entry = ttk.Entry(rows_frame, textvariable=self.custom_cutoff_raw_var, width=10)
        self.custom_cutoff_raw_entry.grid(row=0, column=1, padx=5, pady=3)
        self.custom_cutoff_amps_entry = ttk.Entry(rows_frame, textvariable=self.custom_cutoff_amps_var, width=10)
        self.custom_cutoff_amps_entry.grid(row=0, column=2, padx=5, pady=3)
        ttk.Label(rows_frame, text="Max current:").grid(row=1, column=0, padx=5, pady=3, sticky="e")
        self.custom_max_raw_entry = ttk.Entry(rows_frame, textvariable=self.custom_max_raw_var, width=10)
        self.custom_max_raw_entry.grid(row=1, column=1, padx=5, pady=3)
        self.custom_max_amps_entry = ttk.Entry(rows_frame, textvariable=self.custom_max_amps_var, width=10)
        self.custom_max_amps_entry.grid(row=1, column=2, padx=5, pady=3)
        ttk.Label(
            self.custom_frame,
            text="1 raw unit is treated as approximately 0.001 A.\nThese values are for protocol simulation only.\nNo real charging current comes from the USB interface."
        ).pack(anchor="w", padx=5, pady=(0, 5))
        self.custom_cutoff_raw_entry.bind("<FocusOut>", self.on_custom_raw_focus_out)
        self.custom_max_raw_entry.bind("<FocusOut>", self.on_custom_raw_focus_out)
        self.custom_cutoff_amps_entry.bind("<FocusOut>", self.on_custom_amps_focus_out)
        self.custom_max_amps_entry.bind("<FocusOut>", self.on_custom_amps_focus_out)
        sim_btn_frame = ttk.Frame(self.sim_tab)
        sim_btn_frame.pack(fill="x", padx=6, pady=(4, 4))
        self.sim_start_btn = ttk.Button(
            sim_btn_frame, text="Start simulation", command=self.start_simulation, state="disabled"
        )
        self.sim_start_btn.pack(side="left", padx=4)
        self.sim_stop_btn = ttk.Button(
            sim_btn_frame, text="Stop simulation", command=self.stop_simulation, state="disabled"
        )
        self.sim_stop_btn.pack(side="left", padx=4)
        self.sim_status_var = tk.StringVar(
            value="Simulation idle. Connect to a battery to enable."
        )
        ttk.Label(self.sim_tab, textvariable=self.sim_status_var).pack(anchor="w", padx=8, pady=(0, 6))
        import webbrowser
        about_frame = ttk.Frame(self.about_tab)
        about_frame.pack(fill="both", expand=True, padx=10, pady=10)
        ttk.Label(
            about_frame, text="M18 Protocol GUI", font=("Segoe UI", 14, "bold")
        ).pack(pady=(0, 10))
        ttk.Label(about_frame, text="Developed by KillaVolt").pack(pady=(0, 5))
        github_link = ttk.Label(
            about_frame,
            text="GitHub Profile: https://github.com/KillaVolt",
            foreground="blue",
            cursor="hand2"
        )
        github_link.pack()
        github_link.bind(
            "<Button-1>",
            lambda e: webbrowser.open("https://github.com/KillaVolt")
        )
        ttk.Label(
            about_frame,
            text="GUI submitted for review — awaiting hardware verification",
            foreground="gray"
        ).pack(pady=(15, 0))
        bottom_frame = ttk.LabelFrame(self, text="Output")
        bottom_frame.pack(fill="both", expand=True, padx=10, pady=(0, 10))
        self.output_text = tk.Text(bottom_frame, wrap="word")
        self.output_text.pack(side="left", fill="both", expand=True)
        scrollbar = ttk.Scrollbar(
            bottom_frame, orient="vertical", command=self.output_text.yview
        )
        scrollbar.pack(side="right", fill="y")
        self.output_text.configure(yscrollcommand=scrollbar.set)
        self.custom_frame.pack_forget()
    # -------------------------------------------------------------------------
    # Utility Logging
    # -------------------------------------------------------------------------
    def log(self, text: str, clear: bool = False):
        if clear:
            self.output_text.delete("1.0", tk.END)
        self.output_text.insert(tk.END, text + "\n")
        self.output_text.see(tk.END)

    def sim_log(self, text: str):
        self.log(f"[SIM] {text}")

    def set_status(self, text: str):
        self.status_var.set(text)

    # -------------------------------------------------------------------------
    # Port Refresh
    # -------------------------------------------------------------------------
    def refresh_ports(self):
        ports = list_ports.comports()
        display_names = []
        self.port_map = {}

        for p in ports:
            label = f"{p.device} — {p.description} ({p.manufacturer or 'Unknown'})"
            display_names.append(label)
            self.port_map[label] = p.device

        self.port_combo["values"] = display_names

        if display_names:
            self.port_combo.current(0)
        else:
            self.port_combo.set("")

        self.set_status("Ports refreshed")

    # -------------------------------------------------------------------------
    # Helpers
    # -------------------------------------------------------------------------
    def require_connection(self) -> bool:
        if self.m18_obj is None:
            messagebox.showwarning("Not connected", "Please connect to a port first.")
            return False
        return True

    def capture_output(self, func, *args, **kwargs) -> str:
        buf = io.StringIO()
        old_stdout = sys.stdout
        try:
            sys.stdout = buf
            func(*args, **kwargs)
        finally:
            sys.stdout = old_stdout
        return buf.getvalue()

    # -------------------------------------------------------------------------
    # Connect / Disconnect
    # -------------------------------------------------------------------------
    def connect_device(self):
        selected = self.port_var.get()
        if not selected:
            messagebox.showwarning("No port", "Please select a serial port first.")
            return

        port = self.port_map.get(selected, selected)

        try:
            self.m18_obj = m18.M18(port)
        except Exception as e:
            self.m18_obj = None
            messagebox.showerror("Connection error", f"Failed to connect to {port}.\n\n{e}")
            self.set_status("Connection failed")
            return

        self.set_status(f"Connected to {port}")
        self.log(f"Connected to {port}", clear=True)

        self.connect_btn.configure(state="disabled")
        self.disconnect_btn.configure(state="normal")
        self.idle_btn.configure(state="normal")
        self.health_btn.configure(state="normal")
        self.clipboard_btn.configure(state="normal")
        self.sim_start_btn.configure(state="normal")

    def disconnect_device(self):
        self.stop_simulation()

        if self.m18_obj is not None:
            try:
                try: self.m18_obj.idle()
                except: pass
                try: self.m18_obj.port.close()
                except: pass
            finally:
                self.m18_obj = None

        self.set_status("Disconnected")
        self.log("Disconnected")

        self.connect_btn.configure(state="normal")
        self.disconnect_btn.configure(state="disabled")
        self.idle_btn.configure(state="disabled")
        self.health_btn.configure(state="disabled")
        self.clipboard_btn.configure(state="disabled")
        self.sim_start_btn.configure(state="disabled")
        self.sim_stop_btn.configure(state="disabled")

    # -------------------------------------------------------------------------
    # Commands
    # -------------------------------------------------------------------------
    def cmd_idle(self):
        if not self.require_connection(): return

        def work():
            try:
                self.m18_obj.idle()
                self.after(0, lambda: self.log("TX now low (<1V). Safe to connect battery."))
                self.after(0, lambda: self.set_status("Idle (TX low)"))
            except Exception as e:
                self.after(0, lambda: messagebox.showerror("Idle error", str(e)))

        threading.Thread(target=work, daemon=True).start()

    def cmd_health(self):
        if not self.require_connection(): return

        def work():
            try:
                output = self.capture_output(self.m18_obj.health)
                def finish():
                    self.log("=== Health report ===", clear=True)
                    self.log(output)
                    self.set_status("Health report complete")
                self.after(0, finish)
            except Exception as e:
                self.after(0, lambda: messagebox.showerror("Health error", str(e)))

        threading.Thread(target=work, daemon=True).start()

    def cmd_clipboard(self):
        if not self.require_connection(): return

        def work():
            try:
                output = self.capture_output(
                    self.m18_obj.read_id,
                    None,
                    True,
                    "raw"
                )
                def finish():
                    self.clipboard_clear()
                    self.clipboard_append(output)
                    self.log("Register data copied to clipboard")
                self.after(0, finish)
            except Exception as e:
                self.after(0, lambda: messagebox.showerror("Clipboard error", str(e)))

        threading.Thread(target=work, daemon=True).start()

    # -------------------------------------------------------------------------
    # Console Executor
    # -------------------------------------------------------------------------
    def run_console_code(self):
        code_str = self.console_text.get("1.0", tk.END).strip()
        if not code_str:
            messagebox.showinfo("No code", "Please enter some code.")
            return

        if not self.require_connection(): return

        def work():
            buf = io.StringIO()
            old_stdout, old_stderr = sys.stdout, sys.stderr
            sys.stdout = buf
            sys.stderr = buf

            try:
                env = {"m": self.m18_obj}
                try:
                    exec(code_str, env, {})
                except Exception:
                    traceback.print_exc()
            finally:
                sys.stdout = old_stdout
                sys.stderr = old_stderr

            output = buf.getvalue()
            def finish():
                self.log("=== Console execution ===")
                self.log(output if output.strip() else "(no output)")
            self.after(0, finish)

        threading.Thread(target=work, daemon=True).start()

    # -------------------------------------------------------------------------
    # Profile Logic
    # -------------------------------------------------------------------------
    def on_profile_changed(self, event=None):
        self.update_profile_display()

    def update_profile_display(self):
        profile = self.sim_profile_var.get()
        if profile in self.sim_profiles:
            cutoff, maxc = self.sim_profiles[profile]
            info = (
                f"Profile '{profile}': Cutoff {cutoff} ({cutoff/1000:.2f}A), "
                f"Max {maxc} ({maxc/1000:.2f}A). Simulation only."
            )
            self.profile_info_var.set(info)
            self.custom_frame.pack_forget()
        else:
            self.profile_info_var.set("Custom profile: adjust fields below.")
            self.custom_frame.pack(fill="x", padx=5, pady=(0, 8))

    def on_custom_raw_focus_out(self, event=None):
        if self._sync_in_progress: return
        self._sync_in_progress = True
        try:
            try:
                val = int(self.custom_cutoff_raw_var.get())
                self.custom_cutoff_amps_var.set(f"{val/1000:.2f}")
            except: pass
            try:
                val = int(self.custom_max_raw_var.get())
                self.custom_max_amps_var.set(f"{val/1000:.2f}")
            except: pass
        finally:
            self._sync_in_progress = False

    def on_custom_amps_focus_out(self, event=None):
        if self._sync_in_progress: return
        self._sync_in_progress = True
        try:
            try:
                amps = float(self.custom_cutoff_amps_var.get())
                self.custom_cutoff_raw_var.set(str(int(round(amps*1000))))
            except: pass
            try:
                amps = float(self.custom_max_amps_var.get())
                self.custom_max_raw_var.set(str(int(round(amps*1000))))
            except: pass
        finally:
            self._sync_in_progress = False

    def get_profile_currents(self):
        p = self.sim_profile_var.get()
        if p in self.sim_profiles:
            return self.sim_profiles[p]
        # custom
        try:
            c = int(self.custom_cutoff_raw_var.get())
            m = int(self.custom_max_raw_var.get())
            if c < 0 or m <= 0 or c > 20000 or m > 20000:
                raise ValueError
        except:
            raise ValueError("Invalid custom values.")
        return c, m

    # -------------------------------------------------------------------------
    # Simulation
    # -------------------------------------------------------------------------
    def start_simulation(self):
        if not self.require_connection(): return

        if self.sim_thread and self.sim_thread.is_alive():
            messagebox.showinfo("Running", "Simulation already running.")
            return

        try:
            duration = float(self.sim_duration_var.get())
            if duration <= 0:
                raise ValueError
        except:
            messagebox.showwarning("Invalid duration", "Enter a positive number.")
            return

        sim_baud = self.sim_baud_var.get()
        interval_map = {
            "1200": 1.0,
            "2400": 0.75,
            "4800": 0.5,
            "9600": 0.25
        }
        keepalive_interval = interval_map.get(sim_baud, 0.5)

        try:
            cutoff, maxc = self.get_profile_currents()
        except Exception as e:
            messagebox.showwarning("Profile error", str(e))
            return

        pname = self.sim_profile_var.get()

        self.sim_stop_event = threading.Event()
        self.sim_thread = threading.Thread(
            target=self._simulation_worker,
            args=(duration, keepalive_interval, pname, cutoff, cutoff/1000, maxc, maxc/1000),
            daemon=True
        )
        self.sim_thread.start()

        self.sim_start_btn.configure(state="disabled")
        self.sim_stop_btn.configure(state="normal")
        self.sim_status_var.set(f"Simulation running ({pname}, baud {sim_baud})")

        self.sim_log(
            f"Start {duration}s, {pname}, cutoff={cutoff} ({cutoff/1000:.2f}A), "
            f"max={maxc} ({maxc/1000:.2f}A), interval={keepalive_interval}s"
        )

    def stop_simulation(self):
        if self.sim_stop_event:
            self.sim_stop_event.set()

    def _simulation_worker(self, duration, interval, pname, cutoff_raw, cutoff_amps, max_raw, max_amps):
        m = self.m18_obj
        if m is None:
            self.after(0, lambda: messagebox.showerror("Simulation error", "No connection."))
            return

        old_cut = getattr(m, "CUTOFF_CURRENT", None)
        old_max = getattr(m, "MAX_CURRENT", None)
        m.CUTOFF_CURRENT = cutoff_raw
        m.MAX_CURRENT = max_raw

        start = time.time()

        try:
            self.after(0, lambda: self.sim_log("Reset + initial negotiate..."))
            try:
                m.reset()
            except Exception as e:
                self.after(0, lambda: self.sim_log(f"reset() failed: {e}"))
                return

            try:
                m.configure(2)
                m.get_snapchat()
                time.sleep(0.6)
                m.keepalive()
                m.configure(1)
                m.get_snapchat()
            except Exception as e:
                self.after(0, lambda: self.sim_log(f"configure failed: {e}"))
                return

            while not self.sim_stop_event.is_set():
                elapsed = time.time() - start
                if elapsed >= duration:
                    break

                try:
                    time.sleep(interval)
                    m.keepalive()
                    self.after(0, lambda e=elapsed: self.sim_log(f"Keepalive t={e:.1f}s"))
                except Exception as e:
                    self.after(0, lambda: self.sim_log(f"keepalive() failed: {e}"))
                    break

        finally:
            try: m.idle()
            except: pass

            if old_cut is not None: m.CUTOFF_CURRENT = old_cut
            if old_max is not None: m.MAX_CURRENT = old_max

            def finish():
                self.sim_log("Simulation finished. Back to idle.")
                self.sim_status_var.set("Simulation idle.")
                self.sim_start_btn.configure(state="normal")
                self.sim_stop_btn.configure(state="disabled")
            self.after(0, finish)

# -------------------------------------------------------------------------
# Main loop
# -------------------------------------------------------------------------
if __name__ == "__main__":
    app = M18GUI()
    app.mainloop()
