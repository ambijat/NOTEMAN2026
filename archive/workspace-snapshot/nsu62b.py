from tkinter import *
from tkinter import messagebox
from PIL import Image, ImageTk
from io import BytesIO
import pytesseract
from tkinter import filedialog
from os import walk
import os
import time
import datetime
import subprocess
import numpy as np
import re

br = str('\n\n')

# State flags to enforce EXPORT and RESET sequence after CLP_OCR
needs_export = False
needs_reset = False

root = Tk()
root.title("NoteMaker6")
root.resizable(width=False, height=False)

f0 = Frame(root)
f0.grid(row=0, column=0, sticky='NSEW')


# +++++++++++++++++
# functions

def natural_sort_key(s: str):
    """Return a key for natural sorting of strings containing numbers."""
    return [int(text) if text.isdigit() else text.lower() for text in re.split(r'(\d+)', s)]


def grab_clipboard_image(timeout=2):
    """Return a PIL Image from the clipboard or None if unavailable."""
    cmds = [
        ["xclip", "-selection", "clipboard", "-t", "image/png", "-o"],
        ["wl-paste", "-t", "image/png"]
    ]
    for cmd in cmds:
        try:
            p = subprocess.run(cmd, capture_output=True, timeout=timeout)
            if p.returncode == 0 and p.stdout:
                return Image.open(BytesIO(p.stdout))
        except (FileNotFoundError, subprocess.TimeoutExpired):
            continue
        except Exception:
            continue
    return None


def ready(event):
    if tb6.get() == "Folder Name Here":
        tb6.delete(0, "end")
        tb6.insert(0, '')


def setback(event):
    if tb6.get() == '':
        tb6.insert(0, "Folder Name Here")


def ready2(event):
    if tb4.get() == 'Reference...':
        tb4.delete(0, "end")
        tb4.insert(0, '')


def setback2(event):
    if tb4.get() == '':
        tb4.insert(0, 'Reference...')


def ready3(event):
    if tb5.get() == 'Pgs.':
        tb5.delete(0, "end")
        tb5.insert(0, '')


def setback3(event):
    pg = tb5.get()
    if pg == '':
        tb5.insert(0, 'Pgs.')
        return
    if pg != '' and pg.isdigit():
        pass
    else:
        messagebox.showinfo(title="PROCEDURAL ERROR", message="Invalid Input.")
        tb5.delete(0, END)
        tb5.insert(0, 'Pgs.')


def paste():
    rfi = tb4.get() + "{" + tb5.get() + "}\n"
    try:
        text = root.selection_get(selection='CLIPBOARD') \
            .replace('-\n', '').replace('\n', ' ').encode("ascii", 'ignore')
        tb1.insert('insert', rfi)
        tb1.insert('insert', text)
        tb1.insert('insert', br)
        tb1.clipboard_clear()
    except Exception:
        messagebox.showinfo(message="Clipboard is Empty.")


def export():
    """Write the contents of the text box to a file inside the working folder."""
    cf = tb2.get().replace('\n', '')
    hn = tb6.get().replace('\n', '')
    fc = tb3.get().replace('\n', '')
    tim = tb1.get(1.0, END)
    ds = tb1.get(1.0, 2.0).replace('\n', '')

    if len(cf) >= 1 and len(hn) >= 1 and len(fc) >= 1:
        ff = os.path.join(foloc, fc)
    else:
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Press FOLDER & File.")
        return

    if len(tim) == 0:
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Nothing to Export.")
        return
    else:
        if len(ff) >= 2 and len(tim) > 1 and ds.find("Content Exported to") == -1:
            mode = 'a' if os.path.exists(ff) else 'w'
            with open(ff, mode) as sf:
                sf.write(tim)
            tb1.delete(1.0, END)
            oki = "Content Exported to\n" + ff + "\nPress RESET."
            tb1.insert('insert', oki)
            global needs_export, needs_reset
            needs_export = False
            needs_reset = True
        else:
            messagebox.showinfo(title="PROCEDURAL ERROR",
                                message="Nothing to Export.\nPress reset.")


def reset():
    try:
        ds = tb1.get(1.0, 2.0).replace('\n', '')
        if ds != "" and ds.find("Content Exported to") == -1:
            answer = messagebox.askyesno("Attention", "Do you really want to cleanup?")
            if answer is True:
                tb1.delete(1.0, END)
        else:
            tb1.delete(1.0, END)
        global needs_export, needs_reset
        needs_export = False
        needs_reset = False
    except Exception:
        pass


def folder():
    global foloc
    ff = tb6.get().replace('\n', '')
    if ff == "Folder Name Here" or ff == "":
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Set Folder Name First.\n And, Click Folder.")
    else:
        if ff != "Folder Name Here":
            tb2.delete(0, END)
            answer = filedialog.askdirectory(parent=root,
                                             initialdir=os.getcwd(), title="Please select a folder:")
            gd = "Working Location : "
            if answer != ():
                global foloc
                foloc = str(answer + "/" + tb6.get())
                tb2.insert('insert', gd + foloc)
                if not os.path.exists(foloc):
                    os.makedirs(foloc)
                    messagebox.showinfo(title="TASK DONE", message="FOLDER created.")
                else:
                    messagebox.showinfo(title="TASK DONE", message="FOLDER already exists.")
            else:
                messagebox.showinfo(title="PROCEDURAL ERROR", message="No Folder Selected.")


def newnote():
    tb3.delete(0, END)
    st = time.time()
    ts = datetime.datetime.fromtimestamp(st).strftime('%y%m%d%H%M')
    fc = "note" + str(ts) + ".txt"
    tb3.insert('insert', fc)


def opennote():
    """Open the current note file with the system's default editor."""
    fn = os.path.join(foloc, tb3.get())
    try:
        subprocess.Popen(["xdg-open", fn])
    except FileNotFoundError:
        messagebox.showinfo(title="PROCEDURAL ERROR", message="Note file not found.")


def clpocr():
    """Read image from the clipboard, display it, and append OCR text."""
    rfi = tb4.get() + "{" + tb5.get() + "}\n"
    b = len(tb6.get())
    if tb6.get() == "Folder Name Here" or b < 2:
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Set FOLDER & NEW_NOTE.")
        return

    global needs_export, needs_reset
    if needs_export:
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Please EXPORT before using CLP_OCR again.")
        return
    if needs_reset:
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Please RESET before using CLP_OCR again.")
        return

    img = grab_clipboard_image()
    if img is None:
        messagebox.showinfo(title="TASK DONE", message="No Image on Clipboard Found.")
        return

    if isinstance(img, Image.Image):
        rd = pytesseract.image_to_string(np.array(img), lang='eng') \
            .replace('-\n', '').replace('\n', ' ').encode("ascii", 'ignore')
        tb1.insert('insert', rfi)
        tb1.insert('insert', rd)
        tb1.insert('insert', br)
        tb1.clipboard_clear()

        needs_export = True
        needs_reset = False

        top = Toplevel(root)
        top.title("Clipboard Image")
        imgtk = ImageTk.PhotoImage(img)
        lbl = Label(top, image=imgtk)
        lbl.image = imgtk
        lbl.pack()


def ocr_read():
    """Select an image file from the working folder and append OCR text."""
    rfi = tb4.get() + "{" + tb5.get() + "}\n"
    b = len(tb6.get())
    if tb6.get() == "Folder Name Here" or b < 2:
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Set FOLDER & NEW_NOTE.")
        return

    ft = [('Image files', '*.png *.jpg *.jpeg *.bmp *.tif *.tiff')]
    fn = filedialog.askopenfilename(parent=root, initialdir=foloc,
                                    title="Select Image File:", filetypes=ft)
    if not fn:
        return
    try:
        img = Image.open(fn)
    except Exception:
        messagebox.showinfo(title="PROCEDURAL ERROR", message="Cannot open image.")
        return

    rd = pytesseract.image_to_string(np.array(img), lang='eng') \
        .replace('-\n', '').replace('\n', ' ')
    tb1.insert(END, rfi)
    tb1.insert(END, rd)
    tb1.insert(END, br)


def imgps():
    rfi = tb4.get() + "{" + tb5.get() + "}\n"
    b = len(tb6.get())
    if tb6.get() == "Folder Name Here" or b < 2:
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Set FOLDER & NEW_NOTE.")
        return

    count = 0
    a = foloc
    if len(a) > 2:
        xa = []
        for dp, dn, fn in walk(a):
            fn.sort(key=natural_sort_key)
            for s in fn:
                if s.lower().endswith(('.png', '.jpg', '.jpeg', '.bmp', '.tif', '.tiff')):
                    xa.append(os.path.join(dp, s))

        for x in xa:
            rd = pytesseract.image_to_string(Image.open(x), lang='eng') \
                .replace('-\n', '').replace('\n', ' ').encode("ascii", 'ignore')
            tb1.insert('insert', rfi)
            tb1.insert('insert', rd)
            tb1.insert('insert', br)
            count += 1

    else:
        count -= 1
        messagebox.showinfo(title="PROCEDURAL ERROR",
                            message="Set FOLDER & NEW_NOTE.")

    if count == 0:
        messagebox.showinfo(title="TASK DONE", message="No Image Files Found.")
    if count > 0:
        gg = str(count) + " Files Read."
        messagebox.showinfo(title="TASK DONE", message=gg)


def pgn():
    try:
        pg = int(tb5.get().replace('\n', ''))
        if type(pg) is not int:
            messagebox.showinfo(title="PROCEDURAL ERROR", message="Invalid Number.")
        else:
            pg += 1
        tb5.delete(0, END)
        tb5.insert('insert', pg)
    except ValueError:
        messagebox.showinfo(title="PROCEDURAL ERROR", message="Invalid Input.")
        tb5.delete(0, END)


def pgp():
    """Decrease the page number displayed in the entry box."""
    try:
        pg = int(tb5.get().replace('\n', ''))
        if type(pg) is not int:
            messagebox.showinfo(title="PROCEDURAL ERROR", message="Invalid Number.")
        else:
            pg -= 1
            tb5.delete(0, END)
            tb5.insert('insert', pg)
    except ValueError:
        messagebox.showinfo(title="PROCEDURAL ERROR", message="Invalid Input.")
        tb5.delete(0, END)

# +++++++++++++++++++

# ===============COLUMN0

pbtn4 = Button(f0, text="FOLDER", activebackground="aquamarine", activeforeground="green",
               bd="3", bg="powder blue", command=folder, fg="purple", font=('arial', 10, 'bold'))
pbtn4.grid(row=0, column=0, sticky='NSEW')

tb6 = Entry(f0, font=('arial', 10))
tb6.insert(0, "Folder Name Here")
tb6.bind('<FocusIn>', ready)
tb6.bind('<FocusOut>', setback)
tb6.grid(row=1, column=0, sticky='NSEW', padx=(1, 1), pady=(1, 1))

pbtn5 = Button(f0, text="NEW_NOTE", activebackground="slate blue", activeforeground="pale green",
               bd="3", bg="powder blue", command=newnote, fg="purple", font=('arial', 10, 'bold'))
pbtn5.grid(row=2, column=0, sticky='NSEW')

tb3 = Entry(f0, font=('arial', 10))
tb3.grid(row=3, column=0, sticky='NSEW', padx=(1, 1), pady=(1, 1))

pbtn9 = Button(f0, text="OPEN_NOTE", activebackground="slate blue", activeforeground="pale green",
               bd="3", bg="powder blue", command=opennote, fg="purple", font=('arial', 10, 'bold'))
pbtn9.grid(row=4, column=0, sticky='NSEW')

pbtn3 = Button(f0, text="RESET", activebackground="orange", activeforeground="RoyalBlue3",
               bd="3", bg="powder blue", command=reset, fg="purple", font=('arial', 10, 'bold'))
pbtn3.grid(row=5, column=0, sticky='NSEW')

pbtn2 = Button(f0, text="EXPORT", activebackground="brown", activeforeground="yellow",
               bd="3", bg="powder blue", command=export, fg="purple", font=('arial', 10, 'bold'))
pbtn2.grid(row=6, column=0, sticky='NSEW')

pbtn1 = Button(f0, text="PASTE", activebackground="green", activeforeground="red",
               bd="3", bg="pink", command=paste, fg="purple", font=('arial', 12, 'bold'))
pbtn1.grid(row=7, column=0, sticky='NSEW')

tb4 = Entry(f0, font=('arial', 10))
tb4.insert(0, 'Reference...')
tb4.bind('<FocusIn>', ready2)
tb4.bind('<FocusOut>', setback2)
tb4.grid(row=8, column=0, sticky='NSEW', padx=(1, 1), pady=(1, 1))

# Frame for page navigation buttons
pg_frame = Frame(f0, bg="powder blue", bd=0)
pg_frame.grid(row=9, column=0, sticky='NSEW')

pbtn_back = Button(pg_frame, text="<<", activebackground="SeaGreen", activeforeground="lavender",
                   bd="3", bg="powder blue", command=pgp, fg="purple", font=('arial', 10, 'bold'))
pbtn_back.grid(row=0, column=0, sticky='NSEW')

pbtn_forward = Button(pg_frame, text=">>", activebackground="SeaGreen", activeforeground="lavender",
                      bd="3", bg="powder blue", command=pgn, fg="purple", font=('arial', 10, 'bold'))
pbtn_forward.grid(row=0, column=1, sticky='NSEW')

pg_frame.grid_columnconfigure(0, weight=1)
pg_frame.grid_columnconfigure(1, weight=1)

tb5 = Entry(f0, font=('arial', 10))
tb5.insert(0, 'Pgs.')
tb5.bind('<FocusIn>', ready3)
tb5.bind('<FocusOut>', setback3)
tb5.grid(row=10, column=0, sticky='NSEW', padx=(1, 1), pady=(1, 1))

pbtn10 = Button(f0, text="CLP_OCR", activebackground="yellow", activeforeground="RoyalBlue3",
                bd="3", bg="powder blue", command=clpocr, fg="purple", font=('arial', 10, 'bold'))
pbtn10.grid(row=11, column=0, sticky='NSEW')

pbtn6 = Button(f0, text="OCR_READ", activebackground="yellow", activeforeground="RoyalBlue3",
               bd="3", bg="powder blue", command=imgps, fg="purple", font=('arial', 10, 'bold'))
pbtn6.grid(row=12, column=0, sticky='NSEW')

# ===================COLUMN1

tb1 = Text(f0, font=('arial', 10))
tb1.grid(row=0, column=1, rowspan=13, sticky='NSEW', padx=(1, 1), pady=(1, 1))

sbr = Scrollbar(f0)
sbr.config(command=tb1.yview)
tb1.config(yscrollcommand=sbr.set)
sbr.grid(row=0, column=2, rowspan=13, sticky='NSEW', padx=(1, 1), pady=(1, 1))

tb2 = Entry(f0, font=('arial', 10))
tb2.grid(row=12, column=1, sticky='NSEW', padx=(1, 1), pady=(1, 1))
# ===========================

root.mainloop()
