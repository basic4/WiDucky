import sys
import socket
import time
try:
	import pygtk
	pygtk.require("2.0")
except:
	pass
try:
	import gtk
	import gtk.glade
except:
	sys.exit(1)
	

class duckGuiGTK:
	
	def __init__(self):
	
		self.gladefile = "AnotherLayout1.glade"
		sock = socket.socket()
		connected = False
		script_to_load=""
		scriptLines=""
		comline=""
		mapTypex="US"
		self.glade = gtk.Builder()
		self.glade.add_from_file(self.gladefile)
		self.label = self.glade.get_object("lblStatus") 
		self.txtAddress = self.glade.get_object("txtAddress") 
		self.txtCommand = self.glade.get_object("txtCommand") 
		self.txtOutput = self.glade.get_object("txtOutput")
		self.textbuffer1 = self.glade.get_object("textbuffer1")
		self.lblStatus = self.glade.get_object("lblResponse")
		self.table = self.glade.get_object("window1")
		self.scroller = self.glade.get_object("scrolledwindow1")
		self.black = gtk.gdk.color_parse('#010101')
		self.blue = gtk.gdk.color_parse('#0000FF')
		self.red = gtk.gdk.color_parse('#010101')
		self.txtOutput.modify_bg(gtk.STATE_NORMAL,self.black)
		self.lblStatus.modify_fg(gtk.STATE_NORMAL,self.blue)
		self.table.modify_bg(gtk.STATE_NORMAL,gtk.gdk.color_parse('#669999'))
		self.txtOutput.connect("size-allocate",self._autoscroll)
		self.glade.connect_signals(self)
		self.glade.get_object("window1").show()

	def file_chooser(self):
		dialog = gtk.FileChooserDialog("Open..",
                               None,
                               gtk.FILE_CHOOSER_ACTION_OPEN,
                               (gtk.STOCK_CANCEL, gtk.RESPONSE_CANCEL,
                                gtk.STOCK_OPEN, gtk.RESPONSE_OK))
		dialog.set_default_response(gtk.RESPONSE_OK)

		filter = gtk.FileFilter()
		filter.set_name("All files")
		filter.add_pattern("*")
		dialog.add_filter(filter)
		filter = gtk.FileFilter()
		filter.set_name("scripts")
		filter.add_mime_type("text/plain")
		filter.add_pattern("*.txt")
		dialog.add_filter(filter)
		response = dialog.run()
		if response == gtk.RESPONSE_OK:
			self.script_to_load = dialog.get_filename(), 'selected'
		elif response == gtk.RESPONSE_CANCEL:
			self.script_to_load = ""
		dialog.destroy()
		
		
	def openConnection(self):
		self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
		if self.txtAddress.get_text().find(":") > -1:
			adx,ptx = self.txtAddress.get_text().split(":")
			result = self.sock.connect_ex((adx,int(ptx)))
		else:
			result = self.sock.connect_ex((self.txtAddress.get_text(),6673))

		if result == 0:
			self.sock.settimeout(0.04)
			self.label.set_text("ON-LINE")
			self.label.modify_fg(gtk.STATE_NORMAL,self.blue)
			self.connected = True
			time.sleep(0.1)
			return
		else:
			self.label.set_text("OFF-LINE")
			self.label.modify_fg(gtk.STATE_NORMAL,self.red)
			self.connected = False
			return
				
	def closeConnection(self):
		self.sock.close()
		self.label.set_text("OFF-LINE")
		self.label.modify_fg(gtk.STATE_NORMAL,self.red)
		self.connected = False

		
	def on_MainWindow_delete_event(self,widget,event):
		gtk.main_quit()

	def on_btnExit_clicked(self,widget):
		try:	
			self.closeConnection()
		except:
			pass
		gtk.main_quit()
		
		
	def on_btnOpen_clicked(self,widget):
		if len(self.txtAddress.get_text()) > 10:
			self.openConnection()
			gtk.timeout_add(50,self.check_recv_bytes)
			
	def updateOutput(self):
			while True:
				data=self.sock.recv()
				if not data:
					break
				self.txtOutput.set_text(self.txtOutput.get_text() + str(data))
			return

			
	def on_btnSend_clicked(self,widget):
		if len(self.txtCommand.get_text()) > 1:
			if self.connected == True:
				if self.txtCommand.get_text()=="EXEC":
					self.RunLoadedScript()
					return
				comline = self.txtCommand.get_text()
				self.processLine(comline)
				self.txtCommand.set_text("")
				return
				
	def on_btnLoad_clicked(self,widget):
		##print "Load Clicked!"
		self.file_chooser()
		epoint = 0
		if len(self.script_to_load[0]) > 0:
			try:
				print "Attempting to load script" + str(self.script_to_load[0])
				filex = open(self.script_to_load[0],'r')
				self.scriptLines = filex.readlines()
				filex.close()
				self.lblStatus.set_text("Script Loaded - ok")
			except Exception, e:
				print "epoint = " + str(epoint)
				print str(repr(e))
				self.lblStatus.set_text("Error loading script.")
				pass
				
	def RunLoadedScript(self):
			if len(self.scriptLines) > 1:
				m = 1
				self.textbuffer1.insert_at_cursor(">>> SCRIPT BEGINS <<<" + "\n")
				try:
					for line in self.scriptLines:
						line = line.replace("\r\n","")
						line = line.replace("\n","")
						self.processLine(line)
						while gtk.events_pending():
							gtk.main_iteration_do(True)
						time.sleep(0.1)
						m+=1
					self.lblStatus.set_text("Script Execution Completed.")
					self.textbuffer1.insert_at_cursor(">>> SCRIPT COMPLETED <<<" + "\n")
				except Exception, err:
					print "Error runnig script, line:" + str(m) 
					print sys.exc_info()[0]
					print sys.exc_info()[1]
					self.lblStatus.set_text("Error running script, line:" + str(m))
				self.txtCommand.set_text("")
				pass
					
	def getKeyCode(self,subcom):
		return {
            "CTRL": 128,
            "SHIFT": 129,
            "ALT": 130,
			"TAB": 179,
            "GUI": 131,
            "GUI_R": 135,
            "ENTER": 176,
            "ESC": 177,
            "BACKSPACE": 178,
            "INS": 209,
            "DEL": 212,
            "ALTGR": 134,
            "CTRLR": 132,
            "SHIFTR": 133,
            "F1": 134,
            "F2": 195,
            "F3": 196,
            "F4": 197,
            "F5": 198,
            "F6": 199,
            "F7": 200,
            "F8": 201,
            "F9": 202,
            "F10": 203,
            "F11": 204,
            "F12": 205,
			"CAPS_LOCK": 193,
            "PAGE_UP": 211,
            "PAGE_DOWN": 214,
            "UP": 218,
            "DWN": 217,
            "LFT": 216,
            "RHT":215,
        }.get(subcom, -1) 
        
	
	def replaceKey(self,inp):
		#Keyboard code offsets for UK
		yak = ord(inp)
		if( self.mapTypex == "UK" ):
			switcher = {64: 34, 34: 64, 35: 186, 126: 124, 47: 192, 92: 0xec,}
			return switcher.get(yak,yak)
		else:
			return(int(inp))
	
	def processLine(self,comline):
		if comline.startswith("STRING"):
			resultant = comline[7:]
			self.sendCommandData(resultant)
			self.lblStatus.set_text("String sent...")
		elif (comline.startswith("DELAY")):
			##Wait given millsecs
			delTime = int(comline[6:])
			if(delTime > 0):
				time.sleep(delTime/1000)
		elif comline.startswith("ENTER"):
			k = 176
			self.sendData(k)
		elif comline.startswith("COMD"):
			resultant = comline[5:]
			self.sendCommandData(resultant + "\r\n")
			self.lblStatus.set_text("Command sent...")
		elif comline.startswith("VER"):
			self.sendData(250 & 0xff)
		elif comline.startswith("KEY"):
            ##Send a windows ALT key combo (eg. 'ALT + 0124')
            ##Windows ALT keys
			sendData(252 & 0xff);
            ##numberpad keys inputs
			nums = comline[4:]
			nums.replace(" ","")
			nums.replace("\r\n", "")
			for c in nums:
				ky = self.getNumericPad(c)
				self.sendData(ky & 0xff)
            ##signal sequence end
			self.sendData(253 & 0xff)
			self.lblStatus.set_text("Key sent...")
		elif(comline.startswith("MAP")):
			typx = comline[4:]
			typx.replace(" ","")
			typx.replace("\r\n", "")
			self.mapTypex = typx
			self.lblStatus.set_text("Mapping set...")
		elif(comline.startswith("RAW")):
            ##Raw hex data command
			comline = comline[4:]
			comline.replace("\r\n", "")
			comline.replace(" ", "")
			y = int(comline,16)
			self.sendData(y & 0xff)
			self.lblStatus.set_text("Raw Processed...")
		elif(comline.startswith("MODE")):
            ##Set comms mode-->1 ser/ser--> 0 key/ser
			print "changing control mode..."
			comline = comline[5:]
			comline.replace("\r\n", "")
			comline.replace(" ", "")
			y = int(comline)
			if(y > 0):            
                ##set mode 1: Ser / Ser
				self.sendData(255);
				controlMode = 1;         
			else:          
                ##set mode 0: Key / Ser
				self.sendData(249)
				controlMode = 0
			self.lblStatus.set_text("Mode set...")	
        #Compound Command or Special Key
		else:	
			dif = "+";
			if(comline.find(dif) > 0):				
				##Multi-Key Special Character
				print "Multi-key"
				comline.replace("\r\n","")
				parts = comline.split("+")
				self.sendData(251); #signal multi start
				try:
					for part in parts:
						print "part:" + part
						part = part.replace(" ", "")
						if(self.getKeyCode(part) > 0):
							self.sendData(self.getKeyCode(part))
						else:
							self.sendData(self.replaceKey(part))
						time.sleep(0.1)
				except:
					pass
					print "exception in multi-key"
					self.sendData(254); ##signal multi end
				print "end multi command"
				self.sendData(254); ##signal multi end
					   
			else:
				##Single Special Key
				print "Single special key:" + comline
				rems = comline
				rems.replace(" ","")
				if(self.getKeyCode(rems) > 0):
					self.sendData(self.getKeyCode(rems))

		self.textbuffer1.insert_at_cursor(str(comline) + "\n")

	def sendData(self,inx):
		if self.connected==True:
			rex = inx & 0xff
			self.sock.send(chr(rex))
			time.sleep(0.001)
			

	def sendCommandData(self,datax):
		if (self.connected==True):
			for h in datax:
				self.sendData(self.replaceKey(h))

	def on_txtCommand_key_press_event(self,widget,event):
		if gtk.gdk.keyval_name(event.keyval) =='Return':
			self.on_btnSend_clicked(self)
		
		return

	def check_recv_bytes(self):
			if self.connected ==True:
				try:	
					dxr = ""
					##while gtk.events_pending():
					##	gtk.main_iteration()
					recData=self.sock.recv(10)
					if not recData:
						return True
					dxr = str(recData)		
					self.textbuffer1.insert_at_cursor(dxr)
				except socket.timeout, e:
					##print "No Data"
					pass
				except socket.error, e:
					print "socket error!"
					pass
			return True
		
	def _autoscroll(self,*args):
		adj = self.txtOutput.get_vadjustment()
		adj.set_value(adj.get_upper() - adj.get_page_size())
		return
		   
if __name__=="__main__":
	hwg = duckGuiGTK()
	hwg.mapTypex = "US"
	gtk.main()

