import * as React from 'react'
import { styled } from '@mui/material/styles'
import { Link } from 'react-router-dom'
import { useStore } from '../store'
import { observer, Observer } from 'mobx-react-lite'
import {
    Box,
    AppBar,
    Button,
    ButtonGroup,
    Container,
    IconButton,
    List,
    ListItem,
    ListItemText,
    Paper,
    TextField,
    Toolbar,
    Typography,
    Grid,
    Alert,
    AlertTitle
} from '@mui/material'
import { UniversityLocation } from '../components'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import legendImage from './legend.png'

const StudySchedule = styled('table')`
    border-collapse: collapse;
    height: 100%;

    font-size: 11px;
    font-family: Fira Code, Roboto Mono, monospace;

    td, th {
        white-space: nowrap;
    }

    td {
        border: 1px solid silver;
        text-align: right;
    }

    th {
        padding: 4px;
        text-align: left;
    }

    .empty {
        border: none;
    }

    .skill-level {
        background-color: #e0e0e0;
    }

    .skill {
        padding: 0;

        .level {
            display: inline-block;
            padding: .25rem;
            margin: 2px;
            background-color: #e0e0e0;
        }

        .days {
            display: inline-block;
            padding: .25rem;
        }
    }

    .faction {
        padding: 4px;
        padding-top: 16px;
        background-color: white;
        font-family: ${p => p.theme.typography.h3.fontFamily};
        font-size: 150%;
        border: none;
        text-align: left;
    }

    .unit {
        min-width: 100px;
        padding: 4px;
        background-color: white;
    }

    .orders {
        min-width: 50px;
        padding: 4px;
        background-color: white;
    }

    .target {
        min-width: 50px;
        padding: 4px;
        background-color: white;
    }

    .location {
        padding-top: 2rem;
        padding: 4px;
    }
`

export function UniversityPage() {
    const { game } = useStore()
    const { university } = game

    return (
        <Container component='main' maxWidth={false}>
            <Typography variant='h4'>University</Typography>
            <Grid container>
                <Grid item xs={12}>
                    <Alert severity='info'>
                        <AlertTitle>Legend</AlertTitle>
                        <Box sx={{ width: '100%', maxHeight: 128 }} component={'img'} src={legendImage} />
                    </Alert>
                </Grid>
                <Grid item xs={12}>
                    <StudySchedule>
                        { university.locations.map(location => <UniversityLocation key={`${location.region.x},${location.region.y},${location.region.z}`} location={location} />) }
                    </StudySchedule>
                </Grid>
            </Grid>
        </Container>
    );
}
