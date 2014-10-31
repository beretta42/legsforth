


149  constant INTFLG          \ 1     FlexiKey variable
14a  constant NCYLS           \ 2     Device cylinder count (IDE)
14c  constant NHEADS          \ 1     Device head count (IDE)
14d  constant NSECTS          \ 1     Device sector count (IDE)
14e  constant HDFLAG          \ 1     Hard drive active flag
14f  constant DRVSEL          \ 1     LUN (SCSI), Master/Slave (IDE)
150  constant MAXDRV          \ 1     Highest drive number
151  constant IDNUM           \ 1     Device number (SCSI 0-7) (IDE 0-1)


d930  constant  DISKIO          \ 2     universal hard disk input / output
d932  constant  SETUP           \ 2     Setup command packet
d934  constant  BEEP            \ 2     Make a beep sound
d936  constant  DSKCON2         \ 2     DSKCON Re-entry
d938  constant  HDBOFF          \ 3     "The Offset"
d93b  constant  PORT            \ 2     Interface base address
d93d  constant  CCODE           \ 1     IDE: startup delay / SCSI: wakeup delay
d93e  constant  DEFID           \ 1     Default drive no setting on boot
d93f  constant  DWRead          \ 2     low level dwread routine vector
d941  constant  DWWrite         \ 2     low level dwwrite routine vector

    
    done

    