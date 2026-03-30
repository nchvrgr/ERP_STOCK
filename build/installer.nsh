!macro customInit
  nsExec::ExecToLog 'taskkill /IM "ERP Stock.exe" /T /F'
  nsExec::ExecToLog 'taskkill /IM "ApiWeb.exe" /T /F'
  Sleep 1500
!macroend
