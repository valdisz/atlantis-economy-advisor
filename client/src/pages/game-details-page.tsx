import React, { useEffect } from 'react'
import { observer } from 'mobx-react-lite'
import {
    Box, Container, Stack, List, ListItem, ListItemButton, ListItemText, Typography, Paper, Button, Chip, Dialog, DialogActions,
    DialogContent, DialogContentText, DialogTitle, TextField, Alert, CircularProgress, Card, CardContent
} from '@mui/material'
import { styled } from '@mui/material/styles'
import { Link } from 'react-router-dom'
import { GameDetails } from '../components'
import { useLoaderData, useParams } from 'react-router'
import { useStore, Player, TurnState, GameDetailsStore } from '../store'

export default function GameDetailsPage() {
    const store = useLoaderData() as GameDetailsStore;
    return <GameDetails store={store} />
}

interface PlayerListProps {
    items: Player[]
    onClaim?: (player: Player) => void
}

function PlayerList({ items, onClaim }: PlayerListProps) {
    return <List dense disablePadding>
        { items.map(player => <ListItem key={player.number} disablePadding>
            <ListItemButton
                sx={{ justifyContent: 'space-between', gap: 3 }}
                onClick={() => onClaim && onClaim(player)}>
                <Stack direction='row' gap={2} alignItems='center'>
                    <Box sx={{ minWidth: '3ch', textAlign: 'right' }}>
                        <Typography variant='h6'>{player.number}</Typography>
                    </Box>
                    <ListItemText primary={<Typography fontWeight={player.isOwn ? 600 : 400}>{player.name}</Typography>} />
                </Stack>
                <Stack gap={4} direction='row'>
                    <Box>
                        <Typography variant='caption'>Orders History</Typography>
                        <History items={player.turns} prop='orders' />
                    </Box>
                    <Box>
                        <Typography variant='caption'>Times History</Typography>
                        <History items={player.turns} prop='times' />
                    </Box>
                </Stack>
            </ListItemButton>
        </ListItem>) }
    </List>
}

const Indicator = styled(Box)({
    width: '1ch',
    ':hover': {
        position: 'relative',
        transform: 'scale(1.2)'
    }
})

const YesIndicator = styled(Indicator)(({ theme }) => ({
    backgroundColor: theme.palette.success[theme.palette.mode]
}))

const NoIndicator = styled(Indicator)(({ theme }) => ({
    backgroundColor: theme.palette.error[theme.palette.mode]
}))

interface HistoryProps {
    items: TurnState[]
    prop: keyof TurnState
}

function History({ items, prop }: HistoryProps) {
    return <Stack direction='row' gap={.5} sx={{ height: '3ch' }}>
        {items.map(x => {
            const ItemIndicator = x[prop] ? YesIndicator : NoIndicator
            return <ItemIndicator key={x.turnNumber} title={x.turnNumber.toString()} />
        })}
    </Stack>
}

function ClaimFactionPrompt() {
    const store = useStore().gameDetails.claimFaction

    return <Dialog open={store.isOpen} onClose={store.close}>
        <DialogTitle>Claim <strong>{store.factionName} ({store.factionNumber})</strong></DialogTitle>
        <DialogContent>
            { !!store.error && <Alert severity="error" sx={{ mb: 4 }}>{store.error}</Alert> }
            <DialogContentText>
                To claim controle over the <strong>{store.factionName} ({store.factionNumber})</strong> faction provide password that you use to download orders from the game server.
            </DialogContentText>
            <TextField
                autoFocus
                disabled={store.isLoading}
                margin="dense"
                id="password"
                label="Password"
                type="password"
                fullWidth
                variant="outlined"
                value={store.password}
                onChange={store.onPasswordChange}
            />
        </DialogContent>
        <DialogActions>
            <Button disabled={store.isLoading} onClick={store.close}>Cancel</Button>
            <Box sx={{ position: 'relative' }}>
                <Button disabled={store.isLoading} variant='outlined' onClick={store.claim}>Claim</Button>
                { store.isLoading && <CircularProgress size={18}
                    sx={{
                        position: 'absolute',
                        top: '50%',
                        left: '50%',
                        marginTop: '-9px',
                        marginLeft: '-9px',
                    }}
                />
                }
            </Box>
        </DialogActions>
    </Dialog>
}
const ClaimFactionPromptObserved = observer(ClaimFactionPrompt)
