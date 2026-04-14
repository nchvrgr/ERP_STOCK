!macro customInit
  nsExec::ExecToLog 'taskkill /IM "ERP Stock.exe" /T /F'
  nsExec::ExecToLog 'taskkill /IM "Viñedo de la Villa.exe" /T /F'
  nsExec::ExecToLog 'taskkill /IM "Vinedos de la Villa.exe" /T /F'
  nsExec::ExecToLog 'taskkill /IM "Viñedos de la Villa.exe" /T /F'
  nsExec::ExecToLog 'taskkill /IM "ApiWeb.exe" /T /F'
  Sleep 1500
!macroend
